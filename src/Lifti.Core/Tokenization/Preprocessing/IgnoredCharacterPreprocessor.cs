using System.Collections.Generic;

namespace Lifti.Tokenization.Preprocessing
{
    /// <summary>
    /// An implementation of <see cref="IInputPreprocessor"/> that is able to cause certain characters to be ignored in input text.
    /// </summary>
    public class IgnoredCharacterPreprocessor : IInputPreprocessor
    {
        private readonly HashSet<char> ignoreCharacters;

        /// <summary>
        /// Initializes a new instance of <see cref="IgnoredCharacterPreprocessor"/>.
        /// </summary>
        /// <param name="ignoreCharacters">The set of characters to ignore.</param>
        public IgnoredCharacterPreprocessor(IReadOnlyList<char> ignoreCharacters)
        {
            this.ignoreCharacters = new HashSet<char>(ignoreCharacters);
        }

        /// <inheritdoc />
        public PreprocessedInput Preprocess(char input)
        {
            return this.ignoreCharacters.Contains(input) ? PreprocessedInput.Empty : new PreprocessedInput(input);
        }
    }
}
