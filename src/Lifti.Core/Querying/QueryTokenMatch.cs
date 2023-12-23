using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// Provides information about a document that was matched whilst executing a query.
    /// </summary>
    public readonly struct QueryTokenMatch : IEquatable<QueryTokenMatch>
    {
        /// <summary>
        /// Constructs a new <see cref="QueryTokenMatch"/> instance.
        /// </summary>
        public QueryTokenMatch(int documentId, IReadOnlyList<FieldMatch> fieldMatches)
        {
            this.DocumentId = documentId;
            this.FieldMatches = fieldMatches;
        }

        /// <inheritdoc cref="DocumentId"/>
        [Obsolete("Use DocumentId instead")]
        public int ItemId => this.DocumentId;

        /// <summary>
        /// Gets the id of the matched document.
        /// </summary>
        public int DocumentId { get; }

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
            return HashCode.Combine(this.DocumentId, this.FieldMatches);
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
            return this.DocumentId == other.DocumentId &&
                   this.FieldMatches.SequenceEqual(other.FieldMatches);
        }
    }
}
