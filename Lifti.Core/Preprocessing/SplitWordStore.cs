using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    public class SplitWordStore
    {
        private readonly Dictionary<int, List<SplitWord>> materializedWords = new Dictionary<int, List<SplitWord>>(); // Pooling? Configuration for expected unique words per document?

        public void MergeOrAdd(SplitWordHash hash, ReadOnlySpan<char> word, Range location)
        {
            if (this.materializedWords.TryGetValue(hash.HashValue, out var existingEntries))
            {
                foreach (var existingEntry in existingEntries)
                {
                    if (word.SequenceEqual(existingEntry.Word))
                    {
                        existingEntry.AddLocation(location);
                        return;
                    }
                }

                existingEntries.Add(new SplitWord(word, location));
            }
            else
            {
                this.materializedWords.Add(
                    hash.HashValue,
                    new List<SplitWord>()
                    {
                        new SplitWord(word, location)
                    });
            }
        }

        public IList<SplitWord> ToList()
        {
            return materializedWords.Values.SelectMany(v => v).ToList();
        }
    }
}
