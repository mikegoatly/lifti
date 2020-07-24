using System;
using System.Collections.Generic;

namespace Lifti.Tokenization.TextExtraction
{
    /// <summary>
    /// Defines methods for extracting text to be tokenized from a string.
    /// </summary>
    public interface ITextExtractor
    {
        /// <summary>
        /// Extracts relevant text from the given string. Each returned element includes the
        /// offset in the document that the text was extracted from.
        /// </summary>
        IEnumerable<DocumentTextFragment> Extract(ReadOnlyMemory<char> document, int startOffset = 0);
    }
}
