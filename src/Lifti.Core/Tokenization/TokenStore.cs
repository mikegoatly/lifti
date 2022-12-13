using System.Collections.Generic;
using System.Text;

namespace Lifti.Tokenization
{
    /// <summary>
    /// The <see cref="TokenStore"/> class is responsible for capturing tokens during the
    /// tokenization process and merging location lists where a unique token is matched multiple times. 
    /// </summary>
    internal class TokenStore
    {
        private readonly Dictionary<string, Token> materializedTokens = new();

        /// <summary>
        /// Captures a token at a location, merging the token with any locations
        /// it previously matched at.
        /// </summary>
        public Token MergeOrAdd(StringBuilder tokenText, TokenLocation location)
        {
            var text = tokenText.ToString();
            if (materializedTokens.TryGetValue(text, out var token))
            {
                token.AddLocation(location);
            }
            else
            {
                token = new Token(text, location);
                materializedTokens.Add(text, token);
            }

            return token;
        }

        /// <summary>
        /// Converts the set of captured tokens to a list.
        /// </summary>
        public IReadOnlyCollection<Token> ToList()
        {
            return this.materializedTokens.Values;
        }
    }
}
