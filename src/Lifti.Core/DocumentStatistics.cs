using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Lifti
{
    /// <summary>
    /// Statistics derived from an indexed document.
    /// </summary>
    public class DocumentStatistics
    {
        internal DocumentStatistics(byte fieldId, int tokenCount)
        {
            this.TokenCountByField = ImmutableDictionary<byte, int>.Empty.Add(fieldId, tokenCount);
            this.TotalTokenCount = tokenCount;
        }

        internal DocumentStatistics(IReadOnlyDictionary<byte, int> tokenCountByField)
            : this(tokenCountByField, tokenCountByField.Values.Sum())
        {
        }

        internal DocumentStatistics(IReadOnlyDictionary<byte, int> tokenCountByField, int totalTokenCount)
        {
            this.TokenCountByField = tokenCountByField ?? throw new ArgumentNullException(nameof(tokenCountByField));
            this.TotalTokenCount = totalTokenCount;
        }

        /// <summary>
        /// Gets a dictionary containing the token count for each field indexed in the document.
        /// </summary>
        public IReadOnlyDictionary<byte, int> TokenCountByField { get; }

        /// <summary>
        /// Gets the total token count for the document in all indexed fields.
        /// </summary>
        public int TotalTokenCount { get; }
    }
}
