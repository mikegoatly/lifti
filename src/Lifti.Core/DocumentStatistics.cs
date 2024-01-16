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
        internal DocumentStatistics(byte fieldId, int tokenCount)
        {
            this.TokenCountByField = new Dictionary<byte, int>() { { fieldId, tokenCount } };
            this.TotalTokenCount = tokenCount;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DocumentStatistics"/> class.
        /// </summary>
        /// <param name="tokenCountByField">
        /// The token count for each field indexed in the document. The total token count is 
        /// calculated as the sum of all values in the dictionary.
        /// </param>
        internal DocumentStatistics(IReadOnlyDictionary<byte, int> tokenCountByField)
            : this(tokenCountByField, tokenCountByField.Values.Sum())
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DocumentStatistics"/> class.
        /// </summary>
        /// <param name="tokenCountByField">
        /// The token count for each field indexed in the document.
        /// </param>
        /// <param name="totalTokenCount">
        /// The total token count for the document in all indexed fields.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="tokenCountByField"/> is null.
        /// </exception>
        public DocumentStatistics(IReadOnlyDictionary<byte, int> tokenCountByField, int totalTokenCount)
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
