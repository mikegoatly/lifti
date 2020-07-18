using System;
using System.Collections.Immutable;

namespace Lifti
{
    /// <summary>
    /// Describes statistics for the index in aggregate.
    /// </summary>
    public class IndexStatistics
    {
        private IndexStatistics()
        {
            this.TokenCountByField = ImmutableDictionary<byte, long>.Empty;
        }

        internal IndexStatistics(ImmutableDictionary<byte, long> tokenCountByField, long totalTokenCount)
        {
            this.TokenCountByField = tokenCountByField;
            this.TotalTokenCount = totalTokenCount;
        }

        internal static IndexStatistics Empty { get; } = new IndexStatistics();

        /// <summary>
        /// Gets a dictionary containing the token count for each field indexed in the index.
        /// </summary>
        public ImmutableDictionary<byte, long> TokenCountByField { get; }

        /// <summary>
        /// Gets the total token count for all documents in the index.
        /// </summary>
        public long TotalTokenCount { get; }

        internal IndexStatistics Remove(DocumentStatistics documentStatistics)
        {
            return Adjust(documentStatistics, -1);
        }

        internal IndexStatistics Add(DocumentStatistics documentStatistics)
        {
            return Adjust(documentStatistics, 1);
        }

        private IndexStatistics Adjust(DocumentStatistics documentStatistics, int direction)
        {
            if (documentStatistics is null)
            {
                throw new ArgumentNullException(nameof(documentStatistics));
            }

            var updatedFieldTokenCount = this.TokenCountByField;
            foreach (var fieldTokenCount in documentStatistics.TokenCountByField)
            {
                updatedFieldTokenCount.TryGetValue(fieldTokenCount.Key, out var previousCount);
                updatedFieldTokenCount = updatedFieldTokenCount.SetItem(fieldTokenCount.Key, previousCount + (fieldTokenCount.Value * direction));
            }

            return new IndexStatistics(
                updatedFieldTokenCount,
                this.TotalTokenCount + (documentStatistics.TotalTokenCount * direction));
        }
    }
}
