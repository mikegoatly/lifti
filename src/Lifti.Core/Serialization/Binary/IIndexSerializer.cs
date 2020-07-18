using System.IO;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    /// <summary>
    /// Provides methods to serialize and deserialize an index.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of the key in the index.
    /// </typeparam>
    public interface IIndexSerializer<TKey>
    {
        /// <summary>
        /// Serializes an index into a binary format.
        /// </summary>
        /// <param name="snapshot">
        /// A snapshot of the index to serialize.
        /// </param>
        /// <param name="stream">
        /// The stream to serialize into.
        /// </param>
        /// <param name="disposeStream">
        /// Whether the stream should be disposed of after serialization.
        /// </param>
        Task SerializeAsync(FullTextIndex<TKey> snapshot, Stream stream, bool disposeStream = true);

        /// <summary>
        /// Serializes an index into a binary format.
        /// </summary>
        /// <param name="snapshot">
        /// A snapshot of the index to serialize.
        /// </param>
        /// <param name="stream">
        /// The stream to serialize into.
        /// </param>
        /// <param name="disposeStream">
        /// Whether the stream should be disposed of after serialization.
        /// </param>
        Task SerializeAsync(IIndexSnapshot<TKey> snapshot, Stream stream, bool disposeStream = true);

        /// <summary>
        /// Deserializes an index from a binary format into an index.
        /// </summary>
        /// <param name="index">
        /// The index to deserialize into. This must be an initialized, but empty index.
        /// </param>
        /// <param name="stream">
        /// The stream to deserialize from.
        /// </param>
        /// <param name="disposeStream">
        /// Whether the stream should be disposed of after deserialization.
        /// </param>
        Task DeserializeAsync(FullTextIndex<TKey> index, Stream stream, bool disposeStream = true);
    }
}