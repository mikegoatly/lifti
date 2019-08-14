using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    public class TokenStore
    {
        private readonly Dictionary<int, List<Token>> materializedWords = new Dictionary<int, List<Token>>(); // Pooling? Configuration for expected unique words per document?

        public void MergeOrAdd(TokenHash hash, ReadOnlySpan<char> word, Range location)
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

                existingEntries.Add(new Token(word, location));
            }
            else
            {
                this.materializedWords.Add(
                    hash.HashValue,
                    new List<Token>()
                    {
                        new Token(word, location)
                    });
            }
        }

        public IList<Token> ToList()
        {
            return materializedWords.Values.SelectMany(v => v).ToList();
        }
    }
}
