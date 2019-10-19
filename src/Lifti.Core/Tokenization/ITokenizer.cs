using System;
using System.Collections.Generic;

namespace Lifti.Tokenization
{
    public interface ITokenizer : IConfiguredBy<TokenizationOptions>
    {
        IEnumerable<Token> Process(string input);

        IEnumerable<Token> Process(ReadOnlySpan<char> input);

        IEnumerable<Token> Process(IEnumerable<string> input);
    }
}