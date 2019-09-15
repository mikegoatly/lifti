using System.Collections.Generic;

namespace Lifti.Preprocessing
{
    public interface IInputPreprocessorPipeline : IConfiguredBy<TokenizationOptions>
    {
        IEnumerable<char> Process(char input);
    }
}
