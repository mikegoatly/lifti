using Lifti.Tokenization;
using System;

namespace Lifti
{
    public class TokenizationOptionsBuilder
    {
        private readonly TokenizationOptions options = new TokenizationOptions(TokenizerKind.PlainText);

        /// <summary>
        /// Configures the tokenizer to treat the source text as XML, skipping any characters and text contained
        /// withing tags. This will also skip any attributes and attribute text in the XML, i.e. the only text that
        /// will be indexed will text nodes.
        /// </summary>
        public TokenizationOptionsBuilder XmlContent()
        {
            this.options.TokenizerKind = TokenizerKind.XmlContent;
            return this;
        }

        /// <summary>
        /// Configures the tokenizer to split on punctuation characters (e.g. those that match
        /// <see cref="Char.IsPunctuation(char)"/>). This is the default tokenizer behaviour that can
        /// be suppressed by passing <c>false</c> to this method.
        /// </summary>
        public TokenizationOptionsBuilder SplitOnPunctuation(bool splitOnPunctionation = true)
        {
            this.options.SplitOnPunctuation = splitOnPunctionation;
            return this;
        }

        /// <summary>
        /// Configures the tokenizer to be case insensitive. This will cause characters to 
        /// be indexed normalized to their uppercase form. This is the default tokenizer behavior that can
        /// be suppressed by passing <c>false</c> to this method.
        /// </summary>
        public TokenizationOptionsBuilder CaseInsensitive(bool caseInsensitive = true)
        {
            this.options.CaseInsensitive = caseInsensitive;
            return this;
        }

        /// <summary>
        /// Configures the tokenizer to be accent insensitive, normalizing all characters
        /// to a latin-based equivalent. This is the default tokenizer behavior that can
        /// be suppressed by passing <c>false</c> to this method.
        /// </summary>
        public TokenizationOptionsBuilder AccentInsensitive(bool accentInsensitive = true)
        {
            this.options.AccentInsensitive = accentInsensitive;
            return this;
        }

        /// <summary>
        /// Configures the tokenizer to apply word stemming, e.g. de-pluralizing and stripping
        /// endings such as ING from words. Enabling this will cause both case and accent 
        /// insensitivity to be applied.
        /// </summary>
        public TokenizationOptionsBuilder WithStemming(bool stemming = true)
        {
            this.options.Stemming = stemming;
            return this;
        }

        /// <summary>
        /// Configures the tokenizer to split tokens on the specified characters. Use this
        /// if the text you are indexing contains additional characters you need to split on
        /// that is not whitespace or punctuation, e.g. '|'.
        /// </summary>
        public TokenizationOptionsBuilder SplitOnCharacters(params char[] additionalSplitCharacters)
        {
            this.options.AdditionalSplitCharacters = additionalSplitCharacters;
            return this;
        }

        public TokenizationOptions Build()
        {
            return this.options;
        }
    }
}
