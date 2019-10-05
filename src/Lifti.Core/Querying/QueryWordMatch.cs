using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public struct QueryWordMatch : IEquatable<QueryWordMatch>
    {
        public QueryWordMatch(int itemId, IEnumerable<FieldMatch> fieldMatches)
        {
            this.ItemId = itemId;
            this.FieldMatches = fieldMatches as IReadOnlyList<FieldMatch> ?? fieldMatches.ToList();
        }

        public int ItemId { get; }
        public IReadOnlyList<FieldMatch> FieldMatches { get; }

        public override bool Equals(object obj)
        {
            return obj is QueryWordMatch match &&
                   this.Equals(match);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.ItemId, this.FieldMatches);
        }

        public static bool operator ==(QueryWordMatch left, QueryWordMatch right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QueryWordMatch left, QueryWordMatch right)
        {
            return !(left == right);
        }

        public bool Equals(QueryWordMatch other)
        {
            return this.ItemId == other.ItemId &&
                   this.FieldMatches.SequenceEqual(other.FieldMatches);
        }
    }
}
