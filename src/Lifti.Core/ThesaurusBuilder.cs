using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    /// <summary>
    /// A builder capable of defining the synonyms and hypernyms that should be used when expanding words during the
    /// tokenization process.
    /// </summary>
    public class ThesaurusBuilder
    {
        private readonly Dictionary<string, HashSet<string>> synonymLookup = [];
        private readonly Dictionary<string, HashSet<string>> hypernymLookup = [];

        internal ThesaurusBuilder()
        {
        }

        /// <summary>
        /// Adds a set of synonyms to the thesaurus. Each of these terms will be considered equivalent and querying
        /// for any one of them will result in matches for the other. For example adding the synonyms "big" and "large"
        /// will mean that searching for "big" will also match on documents containing the word "large", and visa-versa.
        /// </summary>
        public ThesaurusBuilder WithSynonyms(params string[] synonyms)
        {
            return this.WithSynonyms((ICollection<string>)synonyms);
        }

        /// <inheritdoc cref="WithSynonyms(string[])"/>
        public ThesaurusBuilder WithSynonyms(ICollection<string> synonyms)
        {
            var existingSynonymSets = synonyms
                .Where(x => synonymLookup.ContainsKey(x))
                .Select(x => this.synonymLookup[x])
                .Distinct()
                .ToList();

            // If no existing synonyms were found, add the new synonym set to the lookup
            if (existingSynonymSets.Count == 0)
            {
                var synonymSet = new HashSet<string>(synonyms);
                foreach (var synonym in synonymSet)
                {
                    this.synonymLookup.Add(synonym, synonymSet);
                }
            }
            else
            {
                // Otherwise, combine the synonym sets into one unique list, now including the new words
                var synonymSet = new HashSet<string>(synonyms.Concat(existingSynonymSets.SelectMany(x => x)));
                foreach (var synonym in synonymSet)
                {
                    this.synonymLookup[synonym] = synonymSet;
                }
            }

            return this;
        }

        /// <summary>
        /// Adds a set of hypernyms to the thesaurus for a given word. A hypernym is a word that is more general than another word.
        /// They differ from synonyms in that configuring <code>WithHypernyms("dog", "animal")</code> means that "dog" will be expanded to 
        /// be searchable as "animal" and "dog", but "animal" will not automatically be expanded to be searchable as "dog".
        /// </summary>
        public ThesaurusBuilder WithHypernyms(string word, params string[] hypernyms)
        {
            return this.WithHypernyms(word, (ICollection<string>)hypernyms);
        }

        /// <inheritdoc cref="WithHypernyms(string, IEnumerable{string})"/>
        public ThesaurusBuilder WithHypernyms(string word, IEnumerable<string> hypernyms)
        {
            // Make sure that the resulting list associated to the word includes the word itself
            hypernyms = hypernyms.Concat(new[] { word });

            WithHypernymsImpl(word, hypernyms);

            return this;
        }

        /// <summary>
        /// Adds a set of hyponyms to the thesaurus for a given word. A hyponym is a word that is more specific than another word.
        /// They differ from synonyms in that configuring <code>WithHyponyms("mammal", "dog", "cat")</code> means that "mammal" will 
        /// be expanded to be searchable as "mammal", "dog" and "cat", but searches for "dog" will not return matches for text containing
        /// "cat".
        /// </summary>
        public ThesaurusBuilder WithHyponyms(string word, params string[] hyponyms)
        {
            return this.WithHyponyms(word, (ICollection<string>)hyponyms);
        }

        /// <inheritdoc cref="WithHyponyms(string, IEnumerable{string})"/>
        public ThesaurusBuilder WithHyponyms(string word, IEnumerable<string> hyponyms)
        {
            ArgumentNullException.ThrowIfNull(hyponyms);

            // A hyponym is just a reversed hypernym relationship
            var hypernymsIncludingWord = new string[2];
            hypernymsIncludingWord[0] = word;

            foreach (var hyponym in hyponyms)
            {
                hypernymsIncludingWord[1] = hyponym;
                this.WithHypernymsImpl(hyponym, hypernymsIncludingWord);
            }

            return this;
        }

        private void WithHypernymsImpl(string word, IEnumerable<string> hypernymsIncludingWord)
        {
            if (this.hypernymLookup.TryGetValue(word, out var existingHypernyms))
            {
                this.hypernymLookup[word] = new HashSet<string>(existingHypernyms.Concat(hypernymsIncludingWord));
            }
            else
            {
                this.hypernymLookup.Add(word, new HashSet<string>(hypernymsIncludingWord));
            }
        }

        internal Thesaurus Build(IIndexTokenizer tokenizer)
        {
            var bakedLookup = new Dictionary<string, IReadOnlyList<string>>();

            var distinctKeys = this.synonymLookup.Keys
                .Concat(this.hypernymLookup.Keys)
                .Distinct();

            string Tokenize(string word)
            {
                var processed = tokenizer.Process(word.AsSpan());
                VerifySingleProcessedToken(word, processed);
                return processed.First().Value;
            }

            foreach (var key in distinctKeys)
            {
                IEnumerable<string> words;
                if (this.synonymLookup.TryGetValue(key, out var synonyms))
                {
                    words = synonyms;
                    if (this.hypernymLookup.TryGetValue(key, out var hypernyms))
                    {
                        words = words.Concat(hypernyms);
                    }
                }
                else
                {
                    words = this.hypernymLookup[key];
                }

                // Use the tokenizer to process each of the synonyms for the word
                words = words.Select(Tokenize);

                // Because of tokenization (e.g. stemming) we could end up with the case where multiple
                // keys map to the same baked key. In this case we just merge the new and existing set of synonyms.
                var normalizedKey = Tokenize(key);
                if (bakedLookup.TryGetValue(normalizedKey, out var currentlyBakedSynonyms))
                {
                    words = words.Concat(currentlyBakedSynonyms);
                }

                bakedLookup[normalizedKey] = words.Distinct().ToList();
            }

            return new Thesaurus(bakedLookup);
        }

        private static void VerifySingleProcessedToken(string word, IReadOnlyCollection<Token> processed)
        {
            if (processed.Count > 1)
            {
                throw new LiftiException(ExceptionMessages.ThesaurusEntriesCannotResultInMultipleWords, word, string.Join(",", processed.Select(p => p.Value)));
            }

            if (processed.Count == 0)
            {
                throw new LiftiException(ExceptionMessages.ThesaurusEntriesMustResultInACompleteWord, word);
            }
        }
    }
}
