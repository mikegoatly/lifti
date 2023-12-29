using Lifti.Tokenization;
using System;
using System.Collections.Generic;

namespace Lifti
{
    /// <summary>
    /// Options that can be provided to an <see cref="IIndexTokenizer"/> to configure its behavior.
    /// </summary>
    public class TokenizationOptions
    {
        private bool caseInsensitive = true;
        private bool accentInsensitive = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenizationOptions"/> class.
        /// </summary>
        internal TokenizationOptions()
        {
        }

        /// <summary>
        /// Gets the default configuration of <see cref="TokenizationOptions"/>. This provides a tokenizer that
        /// is accent and case insensitive that splits on punctuation and whitespace, but does <b>not</b> perform word stemming.
        /// </summary>
        public static TokenizationOptions Default { get; } = new TokenizationOptions();

        /// <summary>
        /// Gets a value indicating whether tokens should be split on punctuation in addition to standard 
        /// separator characters. Defaults to <c>true</c>.
        /// </summary>
        public bool SplitOnPunctuation { get; internal set; } = true;

        /// <summary>
        /// Gets any additional characters that should cause tokens to be split. Defaults to an empty array.
        /// </summary>
        public IReadOnlyList<char> AdditionalSplitCharacters { get; internal set; } = Array.Empty<char>();

        /// <summary>
        /// Gets a value indicating whether case insensitivity should be enforced when tokenizing. Defaults to <c>true</c>.
        /// </summary>
        public bool CaseInsensitive
        {
            get => this.caseInsensitive || (this.Stemmer?.RequiresCaseInsensitivity ?? false);
            internal set => this.caseInsensitive = value;
        }

        /// <summary>
        /// Gets a value indicating whether accents should be stripped from characters when tokenizing. Defaults to <c>true</c>.
        /// </summary>
        public bool AccentInsensitive
        {
            get => this.accentInsensitive || (this.Stemmer?.RequiresAccentInsensitivity ?? false);
            internal set => this.accentInsensitive = value;
        }


        /// <summary>
        /// Gets a value indicating whether word stemming should be applied when tokenizing. Setting this value to true 
        /// forces both <see cref="CaseInsensitive"/> and <see cref="AccentInsensitive"/> to be <c>true</c>.
        /// </summary>
        public IStemmer? Stemmer { get; internal set; }

        /// <summary>
        /// Gets the set of characters that should be ignored in any input.
        /// </summary>
        public IReadOnlyList<char> IgnoreCharacters { get; internal set; } = Array.Empty<char>();
    }
}
