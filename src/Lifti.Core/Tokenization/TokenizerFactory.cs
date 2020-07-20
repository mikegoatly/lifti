using System;

namespace Lifti.Tokenization
{
    /// <inheritdoc />
    public class TokenizerFactory : ITokenizerFactory
    {
        /// <inheritdoc />
        public virtual ITokenizer Create(TokenizationOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

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
