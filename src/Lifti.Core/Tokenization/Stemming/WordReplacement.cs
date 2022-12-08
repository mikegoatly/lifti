namespace Lifti.Tokenization.Stemming
{
    /// <summary>
    /// Information about an exception word and what to return if it is matched.
    /// </summary>
    internal readonly struct WordReplacement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WordReplacement"/> struct.
        /// </summary>
        /// <param name="matchWord">The word to match on and return if matched.</param>
        public WordReplacement(string matchWord)
        {
            this.MatchWord = matchWord;
            this.MatchResult = null;
            this.TrimCharacterCount = 0;
        }

        public WordReplacement(string matchWord, int trimCharacterCount)
        {
            this.MatchWord = matchWord;
            this.MatchResult = null;
            this.TrimCharacterCount = trimCharacterCount;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WordReplacement"/> struct.
        /// </summary>
        /// <param name="matchWord">The word to match on.</param>
        /// <param name="matchResult">The result to return if the word is matched.</param>
        public WordReplacement(string matchWord, string matchResult)
        {
            this.MatchWord = matchWord;
            this.MatchResult = matchResult;
            this.TrimCharacterCount = 0;
        }

        /// <summary>
        /// Gets the word to match on.
        /// </summary>
        /// <value>The exception word to match.</value>
        public string MatchWord { get; }

        /// <summary>
        /// Gets the result to return if the exception word is matched.
        /// </summary>
        /// <value>The exception result.</value>
        public string? MatchResult { get; }
        public int TrimCharacterCount { get; }
    }
}
