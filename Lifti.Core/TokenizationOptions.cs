using System;

namespace Lifti
{
    public class TokenizationOptions
    {
        /// <summary>
        /// Whether tokens should be split on punctuation in addition to standard separator characters. Defaults to true.
        /// </summary>
        public bool SplitOnPunctuation { get; set; } = true;

        /// <summary>
        /// Any additional characters that should cause tokens to be split. Defaults to an empty array.
        /// </summary>
        public char[] AdditionalSplitCharacters { get; set; } = Array.Empty<char>();
    }
}
