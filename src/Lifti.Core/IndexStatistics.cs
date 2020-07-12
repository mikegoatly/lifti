using System;
using System.Collections.Immutable;

namespace Lifti
{
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

        public static IndexStatistics Empty { get; } = new IndexStatistics();

        public ImmutableDictionary<byte, long> TokenCountByField { get; }

        public long TotalTokenCount { get; }

        public IndexStatistics Remove(DocumentStatistics documentStatistics)
        {
            return Adjust(documentStatistics, -1);
        }

        public IndexStatistics Add(DocumentStatistics documentStatistics)
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
