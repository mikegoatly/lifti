using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Lifti.Querying
{
    public struct ScoredToken : IEquatable<ScoredToken>
    {
        public ScoredToken(int itemId, IReadOnlyList<ScoredFieldMatch> fieldMatches)
        {
            this.ItemId = itemId;
            this.FieldMatches = fieldMatches;
        }

        public int ItemId { get; }
        public IReadOnlyList<ScoredFieldMatch> FieldMatches { get; }

        public override bool Equals(object obj)
        {
            return obj is ScoredToken match &&
                   this.Equals(match);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.ItemId, this.FieldMatches);
        }

        public static bool operator ==(ScoredToken left, ScoredToken right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ScoredToken left, ScoredToken right)
        {
            return !(left == right);
        }

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
