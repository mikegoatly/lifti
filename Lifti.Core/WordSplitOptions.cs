using System;

namespace Lifti
{
    public class WordSplitOptions
    {
        /// <summary>
        /// Whether words should be split on punctuation in addition to standard separator characters. Defaults to true.
        /// </summary>
        public bool SplitWordsOnPunctuation { get; set; } = true;

        /// <summary>
        /// Any additional characters that should cause words to be split. Defaults to an empty array.
        /// </summary>
        public char[] AdditionalWordSplitCharacters { get; set; } = Array.Empty<char>();
    }
}
