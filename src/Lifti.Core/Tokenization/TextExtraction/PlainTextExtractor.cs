using System;
using System.Collections.Generic;

namespace Lifti.Tokenization.TextExtraction
{
    /// <summary>
    /// The simplest possible <see cref="ITextExtractor"/> implementation where
    /// all the text passed to <see cref="Extract(ReadOnlyMemory{char},int)"/> is returned
    /// as-is.
    /// </summary>
    public class PlainTextExtractor : ITextExtractor
    {
        /// <inheritdoc />
        public IEnumerable<DocumentTextFragment> Extract(ReadOnlyMemory<char> document, int startOffset)
        {
            yield return new DocumentTextFragment(startOffset, document);
        }
    }
}
