using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Lifti.Querying
{
    /// <summary>
    /// Provides information about an item that was matched and scored whilst executing a query.
    /// </summary>
    public readonly struct ScoredToken : IEquatable<ScoredToken>
    {
        /// <summary>
        /// Constructs a new instance of <see cref="ScoredToken"/>.
        /// </summary>
        public ScoredToken(int itemId, IReadOnlyList<ScoredFieldMatch> fieldMatches)
        {
            this.ItemId = itemId;
            this.FieldMatches = fieldMatches;
        }

        /// <summary>
        /// Gets the id of the item that was matched.
        /// </summary>
        public int ItemId { get; }

        /// <summary>
        /// Gets the fields in which the tokens were matched.
        /// </summary>
        public IReadOnlyList<ScoredFieldMatch> FieldMatches { get; }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is ScoredToken match &&
                   this.Equals(match);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.ItemId, this.FieldMatches);
        }

        /// <inheritdoc />
        public static bool operator ==(ScoredToken left, ScoredToken right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc />
        public static bool operator !=(ScoredToken left, ScoredToken right)
        {
            return !(left == right);
        }

        /// <inheritdoc />
        public bool Equals(ScoredToken other)
        {
            return this.ItemId == other.ItemId &&
                   this.FieldMatches.SequenceEqual(other.FieldMatches);
        }

        internal void ToString(StringBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Append("Item: ").AppendLine(this.ItemId.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("Field matches:");
            foreach (var fieldMatch in this.FieldMatches)
            {
                builder.Append("  Field: ").AppendLine(fieldMatch.FieldId.ToString(CultureInfo.InvariantCulture));
                builder.Append("  Score: ").AppendLine(fieldMatch.Score.ToString(CultureInfo.InvariantCulture));
                foreach (var location in fieldMatch.Locations)
                {
                    builder.Append("  ").AppendLine(location.ToString());
                }
            }
        }
    }
}
