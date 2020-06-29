using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Lifti
{
    public class DocumentStatistics
    {
        internal DocumentStatistics(byte fieldId, int wordCount)
        {
            this.WordCountByField = ImmutableDictionary<byte, int>.Empty.Add(fieldId, wordCount);
            this.TotalWordCount = wordCount;
        }

        internal DocumentStatistics(IReadOnlyDictionary<byte, int> wordCountByField)
            : this(wordCountByField, wordCountByField.Values.Sum())
        {
        }

        internal DocumentStatistics(IReadOnlyDictionary<byte, int> wordCountByField, int totalWordCount)
        {
            this.WordCountByField = wordCountByField ?? throw new ArgumentNullException(nameof(wordCountByField));
            this.TotalWordCount = totalWordCount;
        }

        public IReadOnlyDictionary<byte, int> WordCountByField { get; }
        public int TotalWordCount { get; }
    }
}
