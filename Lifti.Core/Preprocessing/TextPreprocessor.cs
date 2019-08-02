using System;

namespace Lifti
{
    public abstract class TextPreprocessor : ITextPreprocessor
    {
        public string Preprocess(string input)
        {
            return this.PreprocessInput(input ?? throw new ArgumentNullException(nameof(input)));
        }

        protected abstract string PreprocessInput(string input);
    }
}
