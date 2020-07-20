using System;
using System.Collections.Generic;

namespace Lifti.Querying
{
    /// <summary>
    /// Information about an item's field that was matched and scored during the execution of a query.
    /// </summary>
    public struct ScoredFieldMatch : IEquatable<ScoredFieldMatch>
    {
        /// <summary>
        /// Constructs a new instance of <see cref="ScoredFieldMatch"/>.
        /// </summary>
        public ScoredFieldMatch(double score, FieldMatch fieldMatch)
        {
            this.Score = score;
            this.FieldMatch = fieldMatch;
        }

        /// <summary>
        /// Gets the score for the matched field.
        /// </summary>
        public double Score { get; }

        /// <summary>
        /// Gets the id of the matched field.
        /// </summary>
        public byte FieldId => this.FieldMatch.FieldId;

        /// <summary>
        /// Gets the locations in the field text at which the token was matched.
        /// </summary>
        public IReadOnlyList<ITokenLocationMatch> Locations => this.FieldMatch.Locations;

        /// <summary>
        /// Gets the <see cref="FieldMatch"/> details for this instance.
        /// </summary>
        public FieldMatch FieldMatch { get; }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is ScoredFieldMatch match &&
                   this.Equals(match);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Score, this.FieldMatch);
        }

        /// <inheritdoc />
        public bool Equals(ScoredFieldMatch other)
        {
            return this.Score == other.Score &&
                   this.FieldMatch.Equals(other.FieldMatch);
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
