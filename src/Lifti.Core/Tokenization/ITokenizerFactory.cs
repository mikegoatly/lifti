namespace Lifti.Tokenization
{
    /// <summary>
    /// Defines methods for creating <see cref="ITokenizer"/> instances.
    /// </summary>
    public interface ITokenizerFactory
    {
        /// <summary>
        /// Creates an <see cref="ITokenizer"/> that meets the requirements of the provided <see cref="TokenizationOptions"/>.
        /// </summary>
        ITokenizer Create(TokenizationOptions options);
    }
}