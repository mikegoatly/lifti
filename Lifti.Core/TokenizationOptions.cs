using Lifti.Tokenization;

namespace Lifti
{
    public struct TokenizationOptions
    {
        public TokenizationOptions(
            TokenizerKind tokenizerKind,
            bool splitOnPunctuation = true,
            char[] additionalSplitCharacters = null,
            bool caseInsensitive = true,
            bool accentInsensitive = true)
        {
            this.TokenizerKind = tokenizerKind;
            this.SplitOnPunctuation = splitOnPunctuation;
            this.AdditionalSplitCharacters = additionalSplitCharacters;
            this.CaseInsensitive = caseInsensitive;
            this.AccentInsensitive = accentInsensitive;
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
        public char[] AdditionalSplitCharacters { get; }

        public bool CaseInsensitive { get; }

        public bool AccentInsensitive { get; }
    }
}
