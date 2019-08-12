using System.Collections.Generic;

namespace Lifti
{
    public interface IInputPreprocessorPipeline
    {
        IEnumerable<char> Process(char input);
    }
}
