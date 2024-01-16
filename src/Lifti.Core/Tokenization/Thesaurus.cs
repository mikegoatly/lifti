using System.Collections.Generic;

namespace Lifti.Tokenization
{
    internal class Thesaurus : IThesaurus
    {
        public Thesaurus(Dictionary<string, IReadOnlyList<string>> wordLookup)
        {
            this.WordLookup = wordLookup;
        }

        public static IThesaurus Empty { get; } = new Thesaurus([]);

        public Dictionary<string, IReadOnlyList<string>> WordLookup { get; }

        public IEnumerable<Token> Process(Token token)
        {
            if (WordLookup.TryGetValue(token.Value, out var alternatives))
            {
                // Return a token match at all the locations for each
                // synonym. This list will include the original token text as well
                foreach (string alternative in alternatives)
                {
                    // We can share the exact locations list here.
                    yield return token.WithAlternativeValue(alternative);
                }
            }
            else
            {
                // No entries in the thesaurus, just return the original token
                yield return token;
            }
        }
    }
}
