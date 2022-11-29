using Lifti.Tokenization;
using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tests.Querying
{
    public class FakeIndexTokenizer : IIndexTokenizer
    {
        private readonly bool normalizeToUppercase;

        public FakeIndexTokenizer(bool normalizeToUppercase = false)
            : this(new TokenizationOptions())
        {
            this.normalizeToUppercase = normalizeToUppercase;
        }

        public FakeIndexTokenizer(TokenizationOptions options)
        {
            this.Options = options;
        }

        public TokenizationOptions Options { get; }

        public bool IsSplitCharacter(char character)
        {
            return char.IsSeparator(character)|| char.IsPunctuation(character);
        }

        public string Normalize(ReadOnlySpan<char> text)
        {
            var result = new string(text);
            if (this.normalizeToUppercase)
            {
                result = result.ToUpper();
            }

            return result;
        }

        public IReadOnlyList<Token> Process(ReadOnlySpan<char> text)
        {
            return new[] { new Token(new string(text), new TokenLocation(0, 0, (ushort)text.Length)) };
        }

        public IReadOnlyList<Token> Process(IEnumerable<DocumentTextFragment> input)
        {
            return new[] { new Token(string.Join("", input.Select(i => i.Text)), new TokenLocation(0, 0, (ushort)input.Sum(i => i.Text.Length))) };
        }
    }
}
