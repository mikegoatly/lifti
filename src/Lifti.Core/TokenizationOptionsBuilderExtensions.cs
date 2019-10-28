using System;

namespace Lifti
{
    internal static class TokenizationOptionsBuilderExtensions
    { 
        public static TokenizationOptions BuildOptionsOrDefault(this Func<TokenizationOptionsBuilder, TokenizationOptionsBuilder> optionsBuilder)
        {
            return optionsBuilder == null ?
                TokenizationOptions.Default :
                optionsBuilder(new TokenizationOptionsBuilder()).Build();
        }
    }
}
