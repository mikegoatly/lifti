using System;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    /// <summary>
    /// Defines methods for loading an index from a source.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of the key in the index.
    /// </typeparam>
    public interface IIndexReader<TKey> : IDisposable
        where TKey : notnull
    {
        /// <summary>
        /// Populates the given <see cref="FullTextIndex{TKey}"/>.
        /// </summary>
        /// <param name="index">
        /// The index to populate. This should be an empty state.
        /// </param>
        Task ReadIntoAsync(FullTextIndex<TKey> index);
    }
}