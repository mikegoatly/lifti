using Lifti.Tokenization;
using System;

namespace Lifti
{
    /// <summary>
    /// A builder capable of creating an <see cref="ITokenizer"/> instance for use in an index.
    /// </summary>
    public class TokenizerBuilder
    {
        private static Func<TokenizationOptions, ITokenizer> defaultTokenizerFactory = o => new Tokenizer(o);

        private bool splitOnPunctuation = true;
        private bool accentInsensitive = true;
        private bool caseInsensitive = true;
        private bool stemming = false;
        private char[]? additionalSplitCharacters;
        private Func<TokenizationOptions, ITokenizer> factory = defaultTokenizerFactory;

        /// <summary>
        /// Configures a specific implementation of <see cref="ITokenizer"/> to be used. Use this
        /// method if you need more control over the tokenization process.
        /// </summary>
        /// <param name="tokenizerFactory">
        /// A delegate capable of creating the required <see cref="ITokenizer"/>.
        /// </param>
        public TokenizerBuilder WithFactory(Func<TokenizationOptions, ITokenizer> tokenizerFactory)
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
        /// Configures the tokenizer to apply word stemming, e.g. de-pluralizing and stripping
        /// endings such as ING from words. Enabling this will cause both case and accent 
        /// insensitivity to be applied.
        /// </summary>
        public TokenizerBuilder WithStemming(bool stemming = true)
        {
            this.stemming = stemming;
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
        /// Builds an <see cref="ITokenizer"/> instance matching the current configuration.
        /// </summary>
        public ITokenizer Build()
        {
            var options = new TokenizationOptions()
            {
                SplitOnPunctuation = this.splitOnPunctuation,
                AccentInsensitive = this.accentInsensitive,
                CaseInsensitive = this.caseInsensitive,
                Stemming = this.stemming
            };

            if (this.additionalSplitCharacters != null)
            {
                options.AdditionalSplitCharacters = this.additionalSplitCharacters;
            }

            return this.factory(options);
        }
    }
}
