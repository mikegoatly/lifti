using Lifti.Tokenization;

namespace Lifti
{
    /// <summary>
    /// Implemented by classes that can provide access to the various <see cref="IIndexTokenizer"/> implementations
    /// used in an <see cref="IFullTextIndex{TKey}"/>.
    /// </summary>
    public interface IIndexTokenizerProvider
    {
        /// <summary>
        /// Gets the default <see cref="IIndexTokenizer"/> implementation that the index will use when one is
        /// not explicitly configured for a field.
        /// </summary>
        IIndexTokenizer DefaultTokenizer { get; }

        /// <summary>
        /// Gets the <see cref="IIndexTokenizer"/> for the given field.
        /// </summary>
        IIndexTokenizer this[string fieldName] { get; }
    }
}