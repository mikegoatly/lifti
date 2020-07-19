namespace Lifti.Tokenization.Preprocessing
{
    /// <summary>
    /// Implemented by classes capable of pre-processing a character before it is used in an index.
    /// This can occur both during the indexing of and searching for text.
    /// </summary>
    public interface IInputPreprocessor
    {
        /// <summary>
        /// Preprocesses the given character.
        /// </summary>
        /// <param name="input">The input character to pre-process.</param>
        /// <returns>
        /// A <see cref="PreprocessedInput"/> instance describing the output of the pre-processing.
        /// </returns>
        PreprocessedInput Preprocess(char input);
    }
}
