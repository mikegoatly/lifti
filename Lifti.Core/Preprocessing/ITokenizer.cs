using System.Collections.Generic;

namespace Lifti
{
    public interface ITokenizer : IConfiguredByOptions
    {
        IEnumerable<Token> Process(string input);
    }
}