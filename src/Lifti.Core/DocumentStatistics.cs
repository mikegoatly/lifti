using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Lifti
{
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

        public IReadOnlyDictionary<byte, int> TokenCountByField { get; }
        public int TotalTokenCount { get; }
    }
}
