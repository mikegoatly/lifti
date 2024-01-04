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
        private ScoredFieldMatch(double score, byte fieldId, IReadOnlyList<ITokenLocationMatch> tokenLocations)
        {
            this.Score = score;
            this.FieldId = fieldId;
            this.Locations = tokenLocations;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ScoredFieldMatch"/> class where the caller is guaranteeing that 
        /// the set of token locations is already sorted.
        /// </summary>
        internal static ScoredFieldMatch CreateFromPresorted(double score, byte fieldId, IReadOnlyList<ITokenLocationMatch> tokenLocations)
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

        internal static ScoredFieldMatch CreateFromUnsorted(double score, byte fieldId, List<ITokenLocationMatch> tokenLocations)
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
        public IReadOnlyList<ITokenLocationMatch> Locations { get; }

        /// <summary>
        /// Enumerates through all the <see cref="Locations"/> and expands them to a set of <see cref="TokenLocation"/>s.
        /// </summary>
        public IReadOnlyList<TokenLocation> GetTokenLocations()
        {
            return this.Locations.SelectMany(l => l.GetLocations())
                .Distinct()
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
