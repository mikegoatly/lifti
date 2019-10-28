namespace Lifti.Tokenization
{
    public class TokenizerFactory : ITokenizerFactory
    {
        public virtual ITokenizer Create(TokenizationOptions options)
        {
            var tokenizer = CreateTokenizer(options.TokenizerKind);
            tokenizer.Configure(options);
            return tokenizer;
        }

        private static ITokenizer CreateTokenizer(TokenizerKind tokenizerKind)
        {
            switch (tokenizerKind)
            {
                case TokenizerKind.PlainText:
                    return new BasicTokenizer();
                case TokenizerKind.XmlContent:
                    return new XmlTokenizer();
                default:
                    throw new LiftiException(ExceptionMessages.UnsupportedTokenizerKind, tokenizerKind);
            }
        }
    }
}
