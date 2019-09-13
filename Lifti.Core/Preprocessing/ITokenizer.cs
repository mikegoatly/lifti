using System.Collections.Generic;

namespace Lifti.Preprocessing
{
    public interface ITokenizer : IConfiguredByOptions
    {
        IEnumerable<Token> Process(string input);
    }
}