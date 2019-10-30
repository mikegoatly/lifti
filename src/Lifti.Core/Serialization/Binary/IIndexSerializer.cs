using System.IO;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    public interface IIndexSerializer<TKey>
    {
        /// <summary>
        /// Serializes the an index into a binary format.
        /// </summary>
        /// <param name="index">
        /// The index to serialize.
        /// </param>
        /// <param name="stream">
        /// The stream to serialize into.
        /// </param>
        /// <param name="disposeStream">
        /// Whether the stream should be disposed of after serialization.
        /// </param>
        Task SerializeAsync(FullTextIndex<TKey> index, Stream stream, bool disposeStream = true);

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