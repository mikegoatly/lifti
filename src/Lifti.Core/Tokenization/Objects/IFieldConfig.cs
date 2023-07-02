using Lifti.Tokenization.TextExtraction;

namespace Lifti.Tokenization.Objects
{
    /// <summary>
    /// Defines information about how text for a field should be processed.
    /// </summary>
    internal interface IFieldConfig
    {
        /// <summary>
        /// Gets the <see cref="IIndexTokenizer"/> to be used for this field.
        /// </summary>
        IIndexTokenizer Tokenizer { get; }

        /// <summary>
        /// Gets the <see cref="ITextExtractor"/> to be used for this field. If this is null then the default text extractor for the index will be used.
        /// </summary>
        ITextExtractor TextExtractor { get; }

        /// <summary>
        /// Gets the <see cref="IThesaurus"/> configured for use with this field.
        /// </summary>
        IThesaurus Thesaurus { get; }
    }
}