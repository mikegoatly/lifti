using System;

namespace Lifti.Preprocessing
{
    public class TokenizerFactory : ITokenizerFactory
    {
        public virtual ITokenizer Create(TokenizationOptions options)
        {
            var tokenizer = this.CreateTokenizer(options.TokenizerKind);
            tokenizer.Configure(options);
            return tokenizer;
        }

        protected ITokenizer CreateTokenizer(TokenizerKind tokenizerKind)
        {
            switch (tokenizerKind)
            {
                case TokenizerKind.Default:
                    return new BasicTokenizer();
                case TokenizerKind.XmlContent:
                    return new XmlTokenizer();
                default:
                    throw new ArgumentException("Unsupported tokenizer kind " + tokenizerKind, nameof(tokenizerKind));
            }
        }
    }
}
