using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Serialization
{
    /// <summary>
    /// Defines methods for loading an index from a source.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of the key in the index.
    /// </typeparam>
    public interface IIndexDeserializer<TKey> : IDisposable
        where TKey : notnull
    {
        /// <summary>
        /// Reconstructs the index from a serialized source.
        /// </summary>
        ValueTask ReadAsync(
            FullTextIndex<TKey> index,
            CancellationToken cancellationToken = default);
    }
}