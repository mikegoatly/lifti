using System;
using System.Collections.Generic;

namespace Lifti.Tokenization.TextExtraction
{
    /// <summary>
    /// An <see cref="ITextExtractor"/> capable of only extracting text from the content
    /// of an XML-like document. Element names, attribute names and values are all ignored.
    /// </summary>
    public class XmlTextExtractor : ITextExtractor
    {
        private enum State
        {
            None = 0,
            ProcessingTag = 1,
            ProcessingAttributeValue = 2
        }

        /// <inheritdoc />
        public IEnumerable<DocumentTextFragment> Extract(ReadOnlyMemory<char> document, int startOffset)
        {
            var state = State.None;
            var textStart = 0;
            var expectedCloseQuoteForAttributeValue = '\0';

            for (var i = 0; i < document.Length; i++)
            {
                var current = document.Span[i];

                switch (state)
                {
                    case State.None:
                        if (current == '<')
                        {
                            if (textStart < i)
                            {
                                // We've hit a new start tag with text preceding it
                                yield return new DocumentTextFragment(
                                    textStart + startOffset,
                                    document.Slice(textStart, i - textStart));
                            }

                            state = State.ProcessingTag;
                        }

                        break;

                    case State.ProcessingTag:
                        switch (current)
                        {
                            case '>':
                                state = State.None;
                                textStart = i + 1;
                                break;
                            case '\'':
                            case '"':
                                expectedCloseQuoteForAttributeValue = current;
                                state = State.ProcessingAttributeValue;
                                break;
                        }

                        break;

                    case State.ProcessingAttributeValue:
                        if (current == expectedCloseQuoteForAttributeValue)
                        {
                            state = State.ProcessingTag;
                        }

                        break;
                }
            }

            if (textStart < document.Length)
            {
                yield return new DocumentTextFragment(
                    textStart + startOffset,
                    document.Slice(textStart, document.Length - textStart));
            }
        }
    }
}
