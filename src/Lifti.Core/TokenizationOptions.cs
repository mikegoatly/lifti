using Lifti.Tokenization;
using System;
using System.Collections.Generic;

namespace Lifti
{
    public class TokenizationOptions
    {
        private bool caseInsensitive = true;
        private bool accentInsensitive = true;

        public TokenizationOptions(TokenizerKind tokenizerKind)
        {
            this.TokenizerKind = tokenizerKind;
        }

        public static TokenizationOptions Default { get; } = new TokenizationOptions(TokenizerKind.Default);

        /// <summary>
        /// Gets the kind of tokenizer that should be used to read from any provided text.
        /// </summary>
        public TokenizerKind TokenizerKind { get; }

        /// <summary>
        /// Gets or sets a value indicating whether tokens should be split on punctuation in addition to standard 
        /// separator characters. Defaults to <c>true</c>.
        /// </summary>
        public bool SplitOnPunctuation { get; set; }

        /// <summary>
        /// Gets or sets any additional characters that should cause tokens to be split. Defaults to an empty array.
        /// </summary>
        public IReadOnlyList<char> AdditionalSplitCharacters { get; set; } = Array.Empty<char>();

        /// <summary>
        /// Gets or sets a value indicating whether case insensitivity should be enforced when tokenizing. Defaults to <c>true</c>.
        /// </summary>
        public bool CaseInsensitive
        {
            get => this.caseInsensitive || this.Stem;
            set => this.caseInsensitive = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether accents should be stripped from characters when tokenizing. Defaults to <c>true</c>.
        /// </summary>
        public bool AccentInsensitive
        {
            get => this.accentInsensitive || this.Stem;
            set => this.accentInsensitive = value;
        }


        /// <summary>
        /// Gets or sets a value indicating whether word stemming should be applied when tokenizing. Setting this value to true 
        /// forces both <see cref="CaseInsensitive"/> and <see cref="AccentInsensitive"/> to be <c>true</c>.
        /// </summary>
        public bool Stem { get; set; }
    }
}
