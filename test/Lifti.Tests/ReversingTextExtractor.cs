using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;

namespace Lifti.Tests
{
    public class ReversingTextExtractor : ITextExtractor
    {
        public IEnumerable<DocumentTextFragment> Extract(ReadOnlyMemory<char> document, int startOffset = 0)
        {
            var reversed = document.Span.ToArray();
            Array.Reverse(reversed);

            return new[]
            {
                new DocumentTextFragment(0, reversed)
            };
        }
    }
}
