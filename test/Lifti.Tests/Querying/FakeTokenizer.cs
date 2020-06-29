using System;
using System.Collections.Generic;
using System.Linq;
using Lifti.Tokenization;

namespace Lifti.Tests.Querying
{
    public class FakeTokenizer : ITokenizer
    {
        public void Configure(TokenizationOptions options)
        {
        }

        public IReadOnlyList<Token> Process(string input)
        {
            return new[] { new Token(input, new WordLocation(0, 0, (ushort)input.Length)) };
        }

        public IReadOnlyList<Token> Process(ReadOnlySpan<char> input)
        {
            return new[] { new Token(input.ToString(), new WordLocation(0, 0, (ushort)input.Length)) };
        }

        public IReadOnlyList<Token> Process(IEnumerable<string> inputs)
        {
            return inputs.Select(s => new Token(s, new WordLocation(1, 0, (ushort)s.Length))).ToList();
        }
    }
}
