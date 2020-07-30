using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lifti.Tokenization
{
    /// <summary>
    /// The <see cref="TokenStore"/> class is responsible for capturing tokens during the
    /// tokenization process and merging location lists where a unique token is matched multiple times. 
    /// </summary>
    internal class TokenStore
    {
        private readonly Dictionary<int, List<Token>> materializedTokens = new Dictionary<int, List<Token>>();

        /// <summary>
        /// Captures a token at a location, merging the token with any locations
        /// it previously matched at.
        /// </summary>
        public void MergeOrAdd(StringBuilder token, TokenLocation location)
        {
            var hash = new TokenHash(token);
            if (this.materializedTokens.TryGetValue(hash.HashValue, out var existingEntries))
            {
                foreach (var existingEntry in existingEntries)
                {
                    if (token.SequenceEqual(existingEntry.Value))
                    {
                        existingEntry.AddLocation(location);
                        return;
                    }
                }

                existingEntries.Add(new Token(token.ToString(), location));
            }
            else
            {
                this.materializedTokens.Add(
                    hash.HashValue,
                    new List<Token>()
                    {
                        new Token(token.ToString(), location)
                    });
            }
        }

        /// <summary>
        /// Converts the set of captured tokens to a list.
        /// </summary>
        public IReadOnlyList<Token> ToList()
        {
            return this.materializedTokens.Values.SelectMany(v => v).ToList();
        }
    }
}
