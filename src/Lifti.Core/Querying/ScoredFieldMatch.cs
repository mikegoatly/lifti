using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// Information about an document's field that was matched and scored during the execution of a query.
    /// </summary>
    public class ScoredFieldMatch : IEquatable<ScoredFieldMatch>
    {
        private readonly IReadOnlyList<TokenLocation>? rawOrderedLocations;
        private readonly IReadOnlyList<ITokenLocation>? interfacedLocations;
        private ScoredFieldMatch(double score, byte fieldId, IReadOnlyList<TokenLocation> tokenLocations)
        {
            this.Score = score;
            this.FieldId = fieldId;
            this.rawOrderedLocations = tokenLocations;
            this.Locations = tokenLocations;
        }

        private ScoredFieldMatch(double score, byte fieldId, IReadOnlyList<ITokenLocation> tokenLocations)
        {
            this.Score = score;
            this.FieldId = fieldId;
            this.interfacedLocations = tokenLocations;
            this.Locations = tokenLocations;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ScoredFieldMatch"/> class where the caller is guaranteeing that 
        /// the set of token locations is already sorted.
        /// </summary>
        internal static ScoredFieldMatch CreateFromPresorted(double score, byte fieldId, IReadOnlyList<TokenLocation> tokenLocations)
        {
            EnsureTokenLocationOrder(tokenLocations);
            return new ScoredFieldMatch(score, fieldId, tokenLocations);
        }

        internal static ScoredFieldMatch CreateFromPresorted(double score, byte fieldId, IReadOnlyList<ITokenLocation> tokenLocations)
        {
            EnsureTokenLocationOrder(tokenLocations);
            return new ScoredFieldMatch(score, fieldId, tokenLocations);
        }

        [Conditional("DEBUG")]
        private static void EnsureTokenLocationOrder(IReadOnlyList<ITokenLocation> tokenLocations)
        {
#if DEBUG
            // Verify that the tokens locations are in token index order
            for (var i = 1; i < tokenLocations.Count; i++)
            {
                if (tokenLocations[i - 1].MinTokenIndex > tokenLocations[i].MinTokenIndex)
                {
                    Debug.Fail("Token locations must be in token index order");
                }
            }
#endif
        }

        internal static ScoredFieldMatch CreateFromUnsorted(double score, byte fieldId, List<ITokenLocation> tokenLocations)
        {
            tokenLocations.Sort((x, y) => x.MinTokenIndex.CompareTo(y.MinTokenIndex));
            return new ScoredFieldMatch(score, fieldId, tokenLocations);
        }

        /// <summary>
        /// Gets the score for the matched field.
        /// </summary>
        public double Score { get; }

        /// <summary>
        /// Gets the id of the matched field.
        /// </summary>
        public byte FieldId { get; }

        /// <summary>
        /// Gets the locations in the field text at which the token was matched.
        /// </summary>
        internal IReadOnlyList<ITokenLocation> Locations { get; }

        /// <summary>
        /// Collects, deduplicates and sorts all the <see cref="TokenLocation"/>s for this instance.
        /// This method is only expected to be called once per instance, so the result of this is not cached.
        /// Multiple calls to this method will result in multiple enumerations of the locations.
        /// </summary>
        public IReadOnlyList<TokenLocation> GetTokenLocations()
        {
            if (this.rawOrderedLocations != null)
            {
                return this.rawOrderedLocations;
            }

            var results = new HashSet<TokenLocation>();

#if !NETSTANDARD
            results.EnsureCapacity(this.interfacedLocations!.Count);
#endif

            foreach (var location in this.interfacedLocations!)
            {
                location.AddTo(results);
            }

            return results
                .OrderBy(l => l.TokenIndex)
                .ToList();
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is ScoredFieldMatch match &&
                   this.Equals(match);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = HashCode.Combine(this.Score, this.FieldId);

            foreach (var location in this.Locations)
            {
                hashCode = HashCode.Combine(hashCode, location);
            }

            return hashCode;
        }

        /// <inheritdoc />
        public bool Equals(ScoredFieldMatch? other)
        {
            return other is not null &&
                this.Score == other.Score &&
                this.FieldId == other.FieldId &&
                this.Locations.SequenceEqual(other.Locations);
        }

        internal static ScoredFieldMatch Merge(ScoredFieldMatch leftField, ScoredFieldMatch rightField)
        {
            if (leftField.rawOrderedLocations != null && rightField.rawOrderedLocations != null)
            {
                return CreateFromPresorted(
                    leftField.Score + rightField.Score,
                    leftField.FieldId,
                    MergeSort(leftField.rawOrderedLocations, rightField.rawOrderedLocations));
            }

            return CreateFromPresorted(
                leftField.Score + rightField.Score,
                leftField.FieldId,
                MergeSort(leftField.Locations, rightField.Locations));
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(ScoredFieldMatch? left, ScoredFieldMatch? right)
        {
            return left?.Equals(right) ?? right is null;
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(ScoredFieldMatch? left, ScoredFieldMatch? right)
        {
            return !(left == right);
        }

        private static List<T> MergeSort<T>(IReadOnlyList<T> left, IReadOnlyList<T> right)
            where T : IComparable<T>
        {
            // When merging we'll compare the values by MinTokenIndex
            var leftCount = left.Count;
            var rightCount = right.Count;
            var results = new List<T>(leftCount + rightCount);

            var leftIndex = 0;
            var rightIndex = 0;

            while (leftIndex < leftCount && rightIndex < rightCount)
            {
                var leftMatch = left[leftIndex];
                var rightMatch = right[rightIndex];

                switch (leftMatch.CompareTo(rightMatch))
                {
                    case -1:
                        results.Add(leftMatch);
                        leftIndex++;
                        break;
                    case 1:
                        results.Add(rightMatch);
                        rightIndex++;
                        break;
                    default:
                        // They're equal, so we deduplicate and just add one
                        results.Add(leftMatch);
                        leftIndex++;
                        rightIndex++;
                        break;
                }
            }

            // Add any remaining matches from the left
            while (leftIndex < leftCount)
            {
                results.Add(left[leftIndex]);
                leftIndex++;
            }

            // Add any remaining matches from the right
            while (rightIndex < rightCount)
            {
                results.Add(right[rightIndex]);
                rightIndex++;
            }

            return results;
        }
    }
}
