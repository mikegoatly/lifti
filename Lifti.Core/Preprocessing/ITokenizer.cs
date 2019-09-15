using System.Collections.Generic;

namespace Lifti.Preprocessing
{
    public interface ITokenizer : IConfiguredBy<TokenizationOptions>
    {
        IEnumerable<Token> Process(string input);
    }
}