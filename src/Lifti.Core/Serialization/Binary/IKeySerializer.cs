using System.IO;

namespace Lifti.Serialization.Binary
{
    /// <summary>
    /// Defines methods for serializing an index key to/from a binary format.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    public interface IKeySerializer<TKey>
    {
        /// <summary>
        /// Writes the given key to a <see cref="BinaryWriter"/>.
        /// </summary>
        void Write(BinaryWriter writer, TKey key);

        /// <summary>
        /// Reads a key from the given <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">
        /// The <see cref="BinaryReader"/> to read from - it will already 
        /// be at the correct location for reading.
        /// </param>
        /// <returns></returns>
        TKey Read(BinaryReader reader);
    }
}
