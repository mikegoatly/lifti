using System.Text;

namespace Lifti.Tokenization
{
    /// <summary>
    /// A stemmer is used to reduce words to their root form. This is used to reduce the number of words
    /// that need to be indexed, and to allow for more effective searching.
    /// </summary>
    public interface IStemmer
    {
        /// <summary>
        /// Gets a value indicating whether the stemmer requires case insensitivity. In this case, words
        /// are guaranteed to be passed to the stemmer in uppercase.
        /// </summary>
        bool RequiresCaseInsensitivity { get; }

        /// <summary>
        /// Gets a value indicating whether the stemmer requires accent insensitivity. In this case, words
        /// are guaranteed to be passed to the stemmer in their accent insensitive form.
        /// </summary>
        bool RequiresAccentInsensitivity { get; }

        /// <summary>
        /// Applies stemming to the word in the given <see cref="StringBuilder"/>.
        /// </summary>
        void Stem(StringBuilder builder);
    }
}