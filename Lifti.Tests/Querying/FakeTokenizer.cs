using System;
using System.Collections.Generic;
using Lifti.Tokenization;

namespace Lifti.Tests.Querying
{
    public class FakeTokenizer : ITokenizer
    {
        public void Configure(TokenizationOptions options)
        {
        }

        public IEnumerable<Token> Process(string input)
        {
            yield return new Token(input, new WordLocation(0, 0, input.Length));
        }

        public IEnumerable<Token> Process(ReadOnlySpan<char> input)
        {
            return new[] { new Token(input.ToString(), new WordLocation(0, 0, input.Length)) };
        }
    }
}
