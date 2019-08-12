using System;
using System.Collections.Generic;
using System.Text;

namespace Lifti.Preprocessing
{
    public class CaseInsensitiveNormalizer : IInputPreprocessor
    {
        public PreprocessedInput Preprocess(char input)
        {
            return new PreprocessedInput(char.ToUpperInvariant(input));
        }
    }
}
