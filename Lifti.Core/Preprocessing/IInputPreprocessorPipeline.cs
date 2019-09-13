using System.Collections.Generic;

namespace Lifti.Preprocessing
{
    public interface IInputPreprocessorPipeline
    {
        IEnumerable<char> Process(char input);
    }
}
