namespace Lifti.Tokenization
{
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

        protected override bool IsWordSplitCharacter(char current)
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

            return base.IsWordSplitCharacter(current);
        }
    }
}
