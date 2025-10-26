using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    /// <summary>
    /// Statistics derived from an indexed document.
    /// </summary>
    public readonly record struct DocumentStatistics
    {
        internal DocumentStatistics(byte fieldId, int tokenCount, int lastTokenIndex)
        {
            this.StatisticsByField = new Dictionary<byte, FieldStatistics>()
            {
                { fieldId, new FieldStatistics(tokenCount, lastTokenIndex) }
            };
            this.TotalTokenCount = tokenCount;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DocumentStatistics"/> class.
        /// </summary>
        /// <param name="statisticsByField">
        /// The statistics for each field indexed in the document. The total token count is 
        /// calculated as the sum of all TokenCount values in the dictionary.
        /// </param>
        internal DocumentStatistics(IReadOnlyDictionary<byte, FieldStatistics> statisticsByField)
            : this(statisticsByField, statisticsByField.Values.Sum(s => s.TokenCount))
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DocumentStatistics"/> class.
        /// </summary>
        /// <param name="statisticsByField">
        /// The statistics for each field indexed in the document.
        /// </param>
        /// <param name="totalTokenCount">
        /// The total token count for the document in all indexed fields.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="statisticsByField"/> is null.
        /// </exception>
        public DocumentStatistics(
            IReadOnlyDictionary<byte, FieldStatistics> statisticsByField,
            int totalTokenCount)
        {
            this.StatisticsByField = statisticsByField ?? throw new ArgumentNullException(nameof(statisticsByField));
            this.TotalTokenCount = totalTokenCount;
        }

        /// <summary>
        /// Gets a dictionary containing the statistics for each field indexed in the document.
        /// </summary>
        public IReadOnlyDictionary<byte, FieldStatistics> StatisticsByField { get; }

        /// <summary>
        /// Gets the total token count for the document in all indexed fields.
        /// </summary>
        public int TotalTokenCount { get; }
    }
}
