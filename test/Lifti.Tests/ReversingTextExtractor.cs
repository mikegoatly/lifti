using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tests
{
    public class ReversingTextExtractor : ITextExtractor
    {
        public IEnumerable<DocumentTextFragment> Extract(ReadOnlyMemory<char> document, int startOffset = 0)
        {
            var reversed = document.Span.ToArray();
            reversed.Reverse();

            return new[]
            {
                new DocumentTextFragment(0, reversed)
            };
        }
    }
}
