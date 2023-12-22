using System;
using System.Collections.Generic;

namespace Lifti
{
    /// <summary>
    /// Describes statistics for the index in aggregate.
    /// </summary>
    public class IndexStatistics
    {
        private readonly Dictionary<byte, long> tokenCountByField;

        internal IndexStatistics()
        {
            this.tokenCountByField = [];
        }

        /// <summary>
        /// Creates a copy of the specified <see cref="IndexStatistics"/> instance and safe to mutate.
        /// </summary>
        internal IndexStatistics(IndexStatistics original)
        {
            this.tokenCountByField = new(original.tokenCountByField);
            this.TotalTokenCount = original.TotalTokenCount;
        }

        internal IndexStatistics(Dictionary<byte, long> tokenCountByField, long totalTokenCount)
        {
            this.tokenCountByField = tokenCountByField;
            this.TotalTokenCount = totalTokenCount;
        }

        /// <summary>
        /// Gets the token count for the specified field.
        /// </summary>
        public long GetFieldTokenCount(byte fieldId)
        {
            if (!this.tokenCountByField.TryGetValue(fieldId, out var tokenCount))
            {
                throw new LiftiException(ExceptionMessages.UnknownField, fieldId);
            }

            return tokenCount;
        }

        /// <summary>
        /// Gets the total token count for all documents in the index.
        /// </summary>
        public long TotalTokenCount { get; private set; }

        /// <summary>
        /// Gets the total number of tokens stored each field in the index.
        /// </summary>
        public IReadOnlyDictionary<byte, long> TokenCountByField => this.tokenCountByField;

        internal void Remove(DocumentStatistics documentStatistics)
        {
            this.Adjust(documentStatistics, -1);
        }

        internal void Add(DocumentStatistics documentStatistics)
        {
            this.Adjust(documentStatistics, 1);
        }

        private void Adjust(DocumentStatistics documentStatistics, int direction)
        {
            if (documentStatistics is null)
            {
                throw new ArgumentNullException(nameof(documentStatistics));
            }

            foreach (var fieldTokenCount in documentStatistics.TokenCountByField)
            {
                this.tokenCountByField.TryGetValue(fieldTokenCount.Key, out var previousCount);
                this.tokenCountByField[fieldTokenCount.Key] = previousCount + (fieldTokenCount.Value * direction);
            }

            this.TotalTokenCount += documentStatistics.TotalTokenCount * direction;
        }
    }
}
