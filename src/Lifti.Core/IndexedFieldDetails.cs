using Lifti.Tokenization;
using Lifti.Tokenization.TextExtraction;

namespace Lifti
{
    /// <summary>
    /// Information about a field that has been configured for indexing.
    /// </summary>
    public record IndexedFieldDetails(byte Id, ITextExtractor TextExtractor, IIndexTokenizer Tokenizer, IThesaurus Thesaurus);
}
