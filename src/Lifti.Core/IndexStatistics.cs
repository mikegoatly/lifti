using System;
using System.Collections.Immutable;

namespace Lifti
{
    public class IndexStatistics
    {
        private IndexStatistics()
        {
            this.WordCountByField = ImmutableDictionary<byte, long>.Empty;
        }

        internal IndexStatistics(ImmutableDictionary<byte, long> wordCountByField, long totalWordCount)
        {
            this.WordCountByField = wordCountByField;
            this.TotalWordCount = totalWordCount;
        }

        public static IndexStatistics Empty { get; } = new IndexStatistics();

        public ImmutableDictionary<byte, long> WordCountByField { get; }

        public long TotalWordCount { get; }

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

            var updatedFieldWordCount = this.WordCountByField;
            foreach (var fieldWordCount in documentStatistics.WordCountByField)
            {
                updatedFieldWordCount.TryGetValue(fieldWordCount.Key, out var previousCount);
                updatedFieldWordCount = updatedFieldWordCount.SetItem(fieldWordCount.Key, previousCount + (fieldWordCount.Value * direction));
            }

            return new IndexStatistics(
                updatedFieldWordCount,
                this.TotalWordCount + (documentStatistics.TotalWordCount * direction));
        }
    }
}
