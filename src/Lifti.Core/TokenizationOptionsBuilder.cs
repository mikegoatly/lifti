using Lifti.Tokenization;
using System;

namespace Lifti
{
    /// <summary>
    /// A builder capable of creating a <see cref="TokenizationOptions"/> instance for use in an index.
    /// </summary>
    public class TokenizationOptionsBuilder
    {
        private TokenizerKind tokenizerKind = TokenizerKind.PlainText;
        private bool splitOnPunctuation = true;
        private bool accentInsensitive = true;
        private bool caseInsensitive = true;
        private bool stemming = false;
        private char[]? additionalSplitCharacters = null;

        /// <summary>
        /// Configures the tokenizer to treat the source text as XML, skipping any characters and text contained
        /// within tags. This will also skip any attributes and attribute text in the XML, i.e. the only text that
        /// will be indexed will text nodes.
        /// </summary>
        public TokenizationOptionsBuilder WithXmlTokenizer()
        {
            this.tokenizerKind = TokenizerKind.XmlContent;
            return this;
        }

        /// <summary>
        /// Configures the tokenizer to split words on punctuation characters (e.g. those that match
        /// <see cref="Char.IsPunctuation(char)"/>). This is the default tokenizer behaviour that can
        /// be suppressed by passing <c>false</c> to this method, in which case only characters explicitly specified
        /// using <see cref="SplitOnCharacters(char[])" /> will be treated as word breaks.
        /// </summary>
        public TokenizationOptionsBuilder SplitOnPunctuation(bool splitOnPunctionation = true)
        {
            this.splitOnPunctuation = splitOnPunctionation;
            return this;
        }

        /// <summary>
        /// Configures the tokenizer to be case insensitive. This will cause characters to 
        /// be indexed normalized to their uppercase form. This is the default tokenizer behavior that can
        /// be suppressed by passing <c>false</c> to this method.
        /// </summary>
        public TokenizationOptionsBuilder CaseInsensitive(bool caseInsensitive = true)
        {
            this.caseInsensitive = caseInsensitive;
            return this;
        }

        /// <summary>
        /// Configures the tokenizer to normalize characters with diacritics common form. e.g. `aigües` and `aigues` will be equivalent, 
        /// as will `laering` and `læring`. This is the default tokenizer behavior that can
        /// be suppressed by passing <c>false</c> to this method.
        /// </summary>
        public TokenizationOptionsBuilder AccentInsensitive(bool accentInsensitive = true)
        {
            this.accentInsensitive = accentInsensitive;
            return this;
        }

        /// <summary>
        /// Configures the tokenizer to apply word stemming, e.g. de-pluralizing and stripping
        /// endings such as ING from words. Enabling this will cause both case and accent 
        /// insensitivity to be applied.
        /// </summary>
        public TokenizationOptionsBuilder WithStemming(bool stemming = true)
        {
            this.stemming = stemming;
            return this;
        }

        /// <summary>
        /// Configures the tokenizer to split tokens on the specified characters. Use this
        /// if the text you are indexing contains additional characters you need to split on
        /// that is not whitespace or punctuation, e.g. '|'.
        /// </summary>
        public TokenizationOptionsBuilder SplitOnCharacters(params char[] additionalSplitCharacters)
        {
            this.additionalSplitCharacters = additionalSplitCharacters;
            return this;
        }

        /// <summary>
        /// Builds a <see cref="TokenizationOptions"/> instance matching the current builder configuration.
        /// </summary>
        public TokenizationOptions Build()
        {
            var options = new TokenizationOptions(this.tokenizerKind)
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

            return options;
        }
    }
}
