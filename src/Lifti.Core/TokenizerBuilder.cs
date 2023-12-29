using Lifti.Tokenization;
using Lifti.Tokenization.Stemming;
using System;

namespace Lifti
{
    /// <summary>
    /// A builder capable of creating an <see cref="IIndexTokenizer"/> instance for use in an index.
    /// </summary>
    public class TokenizerBuilder
    {
        private static readonly Func<TokenizationOptions, IIndexTokenizer> defaultTokenizerFactory = o => new IndexTokenizer(o);

        private bool splitOnPunctuation = true;
        private bool accentInsensitive = true;
        private bool caseInsensitive = true;
        private IStemmer? stemmer;
        private char[]? additionalSplitCharacters;
        private Func<TokenizationOptions, IIndexTokenizer> factory = defaultTokenizerFactory;
        private char[]? ignoreCharacters;

        /// <summary>
        /// Configures a specific implementation of <see cref="IIndexTokenizer"/> to be used. Use this
        /// method if you need more control over the tokenization process.
        /// </summary>
        /// <param name="tokenizerFactory">
        /// A delegate capable of creating the required <see cref="IIndexTokenizer"/>.
        /// </param>
        public TokenizerBuilder WithFactory(Func<TokenizationOptions, IIndexTokenizer> tokenizerFactory)
        {
            this.factory = tokenizerFactory;
            return this;
        }

        /// <summary>
        /// Configures the tokenizer to split words on punctuation characters (e.g. those that match
        /// <see cref="char.IsPunctuation(char)"/>). This is the default tokenizer behaviour that can
        /// be suppressed by passing <c>false</c> to this method, in which case only characters explicitly specified
        /// using <see cref="SplitOnCharacters(char[])" /> will be treated as word breaks.
        /// </summary>
        public TokenizerBuilder SplitOnPunctuation(bool splitOnPunctionation = true)
        {
            this.splitOnPunctuation = splitOnPunctionation;
            return this;
        }

        /// <summary>
        /// Configures the tokenizer to be case insensitive. This will cause characters to 
        /// be indexed normalized to their uppercase form. This is the default tokenizer behavior that can
        /// be suppressed by passing <c>false</c> to this method.
        /// </summary>
        public TokenizerBuilder CaseInsensitive(bool caseInsensitive = true)
        {
            this.caseInsensitive = caseInsensitive;
            return this;
        }

        /// <summary>
        /// Configures the tokenizer to normalize characters with diacritics common form. e.g. `aigües` and `aigues` will be equivalent, 
        /// as will `laering` and `læring`. This is the default tokenizer behavior that can
        /// be suppressed by passing <c>false</c> to this method.
        /// </summary>
        public TokenizerBuilder AccentInsensitive(bool accentInsensitive = true)
        {
            this.accentInsensitive = accentInsensitive;
            return this;
        }

        /// <summary>
        /// Configures the tokenizer to apply word stemming using the default english Porter Stemmer implementation. 
        /// Used to reduce english words to a common root form, i.e. de-pluralizing and stripping endings such as ING from words. 
        /// Enabling this will cause both case and accent insensitivity to be applied.
        /// </summary>
        public TokenizerBuilder WithStemming(bool stemming = true)
        {
            this.stemmer = new PorterStemmer();
            return this;
        }

        /// <summary>
        /// Configures the tokenizer to apply word stemming using the specified stemmer. Depending on the <see cref="IStemmer.RequiresAccentInsensitivity"/>
        /// and <see cref="IStemmer.RequiresCaseInsensitivity"/> properties of the stemmer, accent and case insensitivity may be applied to the index.
        /// </summary>
        /// <param name="stemmer">
        /// The stemmer to use.
        /// </param>
        public TokenizerBuilder WithStemming(IStemmer stemmer)
        {
            this.stemmer = stemmer;
            return this;
        }

        /// <summary>
        /// Configures the tokenizer to split tokens on the specified characters. Use this
        /// if the text you are indexing contains additional characters you need to split on
        /// that is not whitespace or punctuation, e.g. '|'.
        /// </summary>
        public TokenizerBuilder SplitOnCharacters(params char[] additionalSplitCharacters)
        {
            this.additionalSplitCharacters = additionalSplitCharacters;
            return this;
        }

        /// <summary>
        /// Configures the tokenizer to ignore certain characters as it is parsing input.
        /// Ignoring characters will prevent them from acting as split characters, so care needs to be taken 
        /// that your source text doesn't words delimited only by ignored characters, otherwise you may end 
        /// up unexpectedly joining search terms into one. For example, ignoring the <strong>'</strong> 
        /// character will mean that <strong>O'Reilly</strong> will be tokenized as <strong>OReilly</strong>, 
        /// but if your source text also contains <strong>she said'hello'</strong> then <strong>she</strong> and 
        /// <strong>saidhello</strong> will treated as tokens.
        /// </summary>
        public TokenizerBuilder IgnoreCharacters(params char[] ignoreCharacters)
        {
            this.ignoreCharacters = ignoreCharacters;
            return this;
        }

        /// <summary>
        /// Builds an <see cref="IIndexTokenizer"/> instance matching the current configuration.
        /// </summary>
        public IIndexTokenizer Build()
        {
            var options = new TokenizationOptions()
            {
                SplitOnPunctuation = this.splitOnPunctuation,
                AccentInsensitive = this.accentInsensitive,
                CaseInsensitive = this.caseInsensitive,
                Stemmer = this.stemmer
            };

            if (this.ignoreCharacters != null)
            {
                options.IgnoreCharacters = this.ignoreCharacters;
            }

            if (this.additionalSplitCharacters != null)
            {
                options.AdditionalSplitCharacters = this.additionalSplitCharacters;
            }

            return this.factory(options);
        }
    }
}
