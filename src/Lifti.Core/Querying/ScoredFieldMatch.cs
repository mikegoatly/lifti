using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// Information about an document's field that was matched and scored during the execution of a query.
    /// </summary>
    public readonly struct ScoredFieldMatch : IEquatable<ScoredFieldMatch>
    {
        /// <summary>
        /// Constructs a new instance of <see cref="ScoredFieldMatch"/>.
        /// </summary>
        private ScoredFieldMatch(double score, byte fieldId, IReadOnlyList<ITokenLocation> tokenLocations)
        {
            this.Score = score;
            this.FieldId = fieldId;
            this.Locations = tokenLocations;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ScoredFieldMatch"/> class where the caller is guaranteeing that 
        /// the set of token locations is already sorted.
        /// </summary>
        internal static ScoredFieldMatch CreateFromPresorted(double score, byte fieldId, IReadOnlyList<ITokenLocation> tokenLocations)
        {
#if DEBUG
            // Verify that the tokens locations are in token index order
            for (var i = 1; i < tokenLocations.Count; i++)
            {
                if (tokenLocations[i - 1].MinTokenIndex > tokenLocations[i].MinTokenIndex)
                {
                    System.Diagnostics.Debug.Fail("Token locations must be in token index order");
                }
            }
#endif

            return new ScoredFieldMatch(score, fieldId, tokenLocations);
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
            var results = new HashSet<TokenLocation>();

#if !NETSTANDARD
            results.EnsureCapacity(this.Locations.Count);
#endif

            foreach (var location in this.Locations)
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
        public bool Equals(ScoredFieldMatch other)
        {
            return this.Score == other.Score &&
                   this.FieldId == other.FieldId &&
                   this.Locations.SequenceEqual(other.Locations);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(ScoredFieldMatch left, ScoredFieldMatch right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(ScoredFieldMatch left, ScoredFieldMatch right)
        {
            return !(left == right);
        }
    }
}
