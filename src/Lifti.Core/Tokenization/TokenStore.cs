using System;
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
        private readonly Dictionary<ReadOnlyMemory<char>, Token> materializedTokens = new(ReadOnlyMemoryCharComparer.Instance);

        /// <summary>
        /// Captures a token at a location, merging the token with any locations
        /// it previously matched at. This overload accepts a ReadOnlySpan&lt;char&gt; for improved performance.
        /// </summary>
        public Token MergeOrAdd(ReadOnlyMemory<char> tokenText, TokenLocation location)
        {
            if (this.materializedTokens.TryGetValue(tokenText, out var token))
            {
                token.AddLocation(location);
            }
            else
            {
                var text = tokenText.ToString();
                token = new Token(text, location);
                this.materializedTokens.Add(text.AsMemory(), token);
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
