namespace Lifti.Tokenization
{
    /// <summary>
    /// An implementation of <see cref="ITokenizer"/> that just extends the <see cref="BasicTokenizer"/> allowing for only
    /// the text content of XML elements to be tokenized. Element names, attribute names and attribute values are ignored.
    /// </summary>
    public class XmlTokenizer : BasicTokenizer
    {
        private enum State
        { 
            None = 0,
            ProcessingTag = 1,
            ProcessingAttributeValue = 2
        }

        private State state;
        private char expectedCloseQuoteForAttributeValue;

        /// <inheritdoc />
        /// <remarks>
        /// This is used to trick the tokenizer into thinking that while processing inside an XML element, *every* character 
        /// is a split character. Because split characters appearing in sequence cause no tokens to be emitted, this allows 
        /// us to skip over an XML element until we reach the element text.
        /// </remarks>
        protected override bool IsSplitCharacter(char current)
        {
            switch (this.state)
            {
                case State.None:
                    if (current == '<')
                    {
                        state = State.ProcessingTag;
                        return true;
                    }

                    break;

                case State.ProcessingTag:
                    switch (current)
                    {
                        case '>':
                            state = State.None;
                            break;
                        case '\'':
                        case '"':
                            expectedCloseQuoteForAttributeValue = current;
                            state = State.ProcessingAttributeValue;
                            break;
                    }

                    return true;

                case State.ProcessingAttributeValue:
                    if (current == expectedCloseQuoteForAttributeValue)
                    {
                        state = State.ProcessingTag;
                    }

                    return true;
            }

            return base.IsSplitCharacter(current);
        }
    }
}
