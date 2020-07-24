using Lifti.Tokenization;
using System;

namespace Lifti
{
    internal static class TokenizationOptionsBuilderExtensions
    {
        public static ITokenizer? CreateTokenizer(this Func<TokenizationOptionsBuilder, TokenizationOptionsBuilder>? optionsBuilder)
        {
            return optionsBuilder == null ?
                null :
                new Tokenizer(optionsBuilder(new TokenizationOptionsBuilder()).Build());
        }
    }
}
