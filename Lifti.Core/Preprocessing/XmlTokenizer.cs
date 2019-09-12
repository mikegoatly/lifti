namespace Lifti.Preprocessing
{
    public class XmlTokenizer : BasicTokenizer
    {
        private bool processingTag;

        public XmlTokenizer(IInputPreprocessorPipeline inputPreprocessorPipeline)
            : base(inputPreprocessorPipeline)
        {
        }

        protected override bool IsWordSplitCharacter(char current)
        {
            if (this.processingTag)
            {
                if (current == '>')
                {
                    this.processingTag = false;
                }

                return true;
            }

            if (current == '<')
            {
                this.processingTag = true;
                return true;
            }

            return base.IsWordSplitCharacter(current);
        }
    }
}
