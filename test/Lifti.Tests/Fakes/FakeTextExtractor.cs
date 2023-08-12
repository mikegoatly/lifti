using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifti.Tests.Fakes
{
    public class FakeTextExtractor : ITextExtractor
    {
        private readonly DocumentTextFragment[] fragments;

        public FakeTextExtractor(params DocumentTextFragment[] fragments)
        {
            this.fragments = fragments;
        }
        public IEnumerable<DocumentTextFragment> Extract(ReadOnlyMemory<char> document, int startOffset = 0)
        {
            return this.fragments;
        }
    }
}
