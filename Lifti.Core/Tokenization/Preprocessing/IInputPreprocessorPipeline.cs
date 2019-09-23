using System.Collections.Generic;

namespace Lifti.Tokenization.Preprocessing
{
    public interface IInputPreprocessorPipeline : IConfiguredBy<TokenizationOptions>
    {
        IEnumerable<char> Process(char input);
    }
}
