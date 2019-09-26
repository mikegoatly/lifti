using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lifti.Tokenization
{
    internal class TokenStore
    {
        private readonly Dictionary<int, List<Token>> materializedWords = new Dictionary<int, List<Token>>(); // Pooling? Configuration for expected unique words per document?

        public void MergeOrAdd(TokenHash hash, StringBuilder word, WordLocation location)
        {
            if (this.materializedWords.TryGetValue(hash.HashValue, out var existingEntries))
            {
                foreach (var existingEntry in existingEntries)
                {
                    if (word.SequenceEqual(existingEntry.Value))
                    {
                        existingEntry.AddLocation(location);
                        return;
                    }
                }

                existingEntries.Add(new Token(word.ToString(), location));
            }
            else
            {
                this.materializedWords.Add(
                    hash.HashValue,
                    new List<Token>()
                    {
                        new Token(word.ToString(), location)
                    });
            }
        }

        public IList<Token> ToList()
        {
            return this.materializedWords.Values.SelectMany(v => v).ToList();
        }
    }
}
