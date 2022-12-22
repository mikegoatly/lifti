using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// Provides information about an item that was matched whilst executing a query.
    /// </summary>
    public readonly struct QueryTokenMatch : IEquatable<QueryTokenMatch>
    {
        /// <summary>
        /// Constructs a new <see cref="QueryTokenMatch"/> instance.
        /// </summary>
        public QueryTokenMatch(int itemId, IReadOnlyList<FieldMatch> fieldMatches)
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
        public IReadOnlyList<FieldMatch> FieldMatches { get; }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is QueryTokenMatch match &&
                   this.Equals(match);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.ItemId, this.FieldMatches);
        }

        /// <inheritdoc />
        public static bool operator ==(QueryTokenMatch left, QueryTokenMatch right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc />
        public static bool operator !=(QueryTokenMatch left, QueryTokenMatch right)
        {
            return !(left == right);
        }

        /// <inheritdoc />
        public bool Equals(QueryTokenMatch other)
        {
            return this.ItemId == other.ItemId &&
                   this.FieldMatches.SequenceEqual(other.FieldMatches);
        }
    }
}
