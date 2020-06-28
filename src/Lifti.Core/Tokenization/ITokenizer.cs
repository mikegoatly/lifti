using System;
using System.Collections.Generic;

namespace Lifti.Tokenization
{
    public interface ITokenizer : IConfiguredBy<TokenizationOptions>
    {
        IReadOnlyList<Token> Process(string input);

        IReadOnlyList<Token> Process(ReadOnlySpan<char> input);

        IReadOnlyList<Token> Process(IEnumerable<string> input);
    }
}