using Lifti.Tokenization;
using System.Collections.Generic;

namespace Lifti
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Should not be compared")]
    public struct TokenizationOptions
    {
        public TokenizationOptions(
            TokenizerKind tokenizerKind,
            bool splitOnPunctuation = true,
            char[] additionalSplitCharacters = null,
            bool caseInsensitive = true,
            bool accentInsensitive = true,
            bool stem = false)
        {
            this.TokenizerKind = tokenizerKind;
            this.SplitOnPunctuation = splitOnPunctuation;
            this.AdditionalSplitCharacters = additionalSplitCharacters;
            this.CaseInsensitive = caseInsensitive;
            this.AccentInsensitive = accentInsensitive;
            this.Stem = stem;
        }

        public static TokenizationOptions Default { get; } = new TokenizationOptions(TokenizerKind.Default);

        public TokenizerKind TokenizerKind { get; }

        /// <summary>
        /// Whether tokens should be split on punctuation in addition to standard separator characters. Defaults to true.
        /// </summary>
        public bool SplitOnPunctuation { get; }

        /// <summary>
        /// Any additional characters that should cause tokens to be split. Defaults to an empty array.
        /// </summary>
        public IReadOnlyList<char> AdditionalSplitCharacters { get; }

        public bool CaseInsensitive { get; }

        public bool AccentInsensitive { get; }
        public bool Stem { get; }
    }
}
