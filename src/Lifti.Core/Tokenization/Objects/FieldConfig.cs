using Lifti.Tokenization.TextExtraction;

namespace Lifti.Tokenization.Objects
{
    internal abstract class FieldConfig : IFieldConfig
    {
        protected FieldConfig(IIndexTokenizer tokenizer, ITextExtractor textExtractor, IThesaurus thesaurus)
        {
            this.Tokenizer = tokenizer;
            this.TextExtractor = textExtractor;
            this.Thesaurus = thesaurus;

        }
        /// <inheritdoc />
        public IIndexTokenizer Tokenizer { get; }

        /// <inheritdoc />
        public ITextExtractor TextExtractor { get; }

        /// <inheritdoc />
        public IThesaurus Thesaurus { get; }
    }
}