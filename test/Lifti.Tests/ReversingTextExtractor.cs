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
            return new[]
            {
                new DocumentTextFragment(
                    0, 
                    new ReadOnlyMemory<char>(document.ToArray().Reverse().ToArray()))
            };
        }
    }
}
