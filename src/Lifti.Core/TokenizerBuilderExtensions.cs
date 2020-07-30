using Lifti.Tokenization;
using System;

namespace Lifti
{
    internal static class TokenizerBuilderExtensions
    {
        public static ITokenizer? CreateTokenizer(this Func<TokenizerBuilder, TokenizerBuilder>? optionsBuilder)
        {
            return optionsBuilder == null ?
                null :
                optionsBuilder(new TokenizerBuilder()).Build();
        }
    }
}
