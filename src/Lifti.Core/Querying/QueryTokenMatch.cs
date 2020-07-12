using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public struct QueryTokenMatch : IEquatable<QueryTokenMatch>
    {
        public QueryTokenMatch(int itemId, IReadOnlyList<FieldMatch> fieldMatches)
        {
            this.ItemId = itemId;
            this.FieldMatches = fieldMatches;
        }

        public int ItemId { get; }
        public IReadOnlyList<FieldMatch> FieldMatches { get; }

        public override bool Equals(object obj)
        {
            return obj is QueryTokenMatch match &&
                   this.Equals(match);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.ItemId, this.FieldMatches);
        }

        public static bool operator ==(QueryTokenMatch left, QueryTokenMatch right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QueryTokenMatch left, QueryTokenMatch right)
        {
            return !(left == right);
        }

        public bool Equals(QueryTokenMatch other)
        {
            return this.ItemId == other.ItemId &&
                   this.FieldMatches.SequenceEqual(other.FieldMatches);
        }
    }
}
