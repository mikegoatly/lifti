using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public struct QueryWordMatch : IEquatable<QueryWordMatch>
    {
        public QueryWordMatch(int itemId, IEnumerable<IndexedWord> indexedWordLocations)
        {
            this.ItemId = itemId;
            this.IndexedWordLocations = indexedWordLocations.ToList();
        }

        public int ItemId { get; }
        public IReadOnlyList<IndexedWord> IndexedWordLocations { get; }

        public override bool Equals(object obj)
        {
            return obj is QueryWordMatch match &&
                   this.Equals(match);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.ItemId, this.IndexedWordLocations);
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
                   this.IndexedWordLocations.SequenceEqual(other.IndexedWordLocations);
        }
    }
}
