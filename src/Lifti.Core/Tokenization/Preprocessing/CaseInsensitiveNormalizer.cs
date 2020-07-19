namespace Lifti.Tokenization.Preprocessing
{
    /// <summary>
    /// An implementation of <see cref="IInputPreprocessor"/> that normalizes input such that
    /// the index is case-insensitive.
    /// </summary>
    /// <seealso cref="Lifti.Tokenization.Preprocessing.IInputPreprocessor" />
    public class CaseInsensitiveNormalizer : IInputPreprocessor
    {
        /// <inheritdoc />
        public PreprocessedInput Preprocess(char input)
        {
            return new PreprocessedInput(char.ToUpperInvariant(input));
        }
    }
}
