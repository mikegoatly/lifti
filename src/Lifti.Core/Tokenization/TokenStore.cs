using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lifti.Tokenization
{
    internal class TokenStore
    {
        private readonly Dictionary<int, List<Token>> materializedTokens = new Dictionary<int, List<Token>>(); // Pooling? Configuration for expected unique tokens per document?

        public void MergeOrAdd(TokenHash hash, StringBuilder token, TokenLocation location)
        {
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

        public IReadOnlyList<Token> ToList()
        {
            return this.materializedTokens.Values.SelectMany(v => v).ToList();
        }
    }
}
