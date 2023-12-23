using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Lifti.Querying
{
    /// <summary>
    /// Provides information about a document's tokens that were matched and scored whilst executing a query.
    /// </summary>
    public readonly struct ScoredToken : IEquatable<ScoredToken>
    {
        /// <summary>
        /// Constructs a new instance of <see cref="ScoredToken"/>.
        /// </summary>
        public ScoredToken(int documentId, IReadOnlyList<ScoredFieldMatch> fieldMatches)
        {
            this.DocumentId = documentId;
            this.FieldMatches = fieldMatches;
        }

        /// <inheritdoc cref="DocumentId" />
        [Obsolete("Use DocumentId property instead")]
        public int ItemId => this.DocumentId;

        /// <summary>
        /// Gets the id of the document that was matched.
        /// </summary>
        public int DocumentId { get; }

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
            return HashCode.Combine(this.DocumentId, this.FieldMatches);
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
            return this.DocumentId == other.DocumentId &&
                   this.FieldMatches.SequenceEqual(other.FieldMatches);
        }

        internal void ToString(StringBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Append("Document: ").AppendLine(this.DocumentId.ToString(CultureInfo.InvariantCulture));
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
