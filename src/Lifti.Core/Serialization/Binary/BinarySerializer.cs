using System;
using System.IO;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    /// <summary>
    /// An <see cref="IIndexSerializer{TKey}"/> implementation capable of serializing and deserializing
    /// an index to/from a binary representation.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class BinarySerializer<TKey> : IIndexSerializer<TKey>
    {
        private readonly IKeySerializer<TKey> keySerializer;

        /// <summary>
        /// Constructs a new <see cref="BinarySerializer{TKey}"/> instance.
        /// </summary>
        public BinarySerializer()
            : this(StandardKeySerializerFactory.Create<TKey>())
        {
        }

        /// <summary>
        /// Constructs a new <see cref="BinarySerializer{TKey}"/> instance.
        /// </summary>
        /// <param name="keySerializer">
        /// The <see cref="IKeySerializer{TKey}"/> implementation to use when (de)serializing
        /// keys for the index.
        /// </param>
        public BinarySerializer(IKeySerializer<TKey> keySerializer)
        {
            this.keySerializer = keySerializer;
        }

        /// <inheritdoc/>
        public async Task SerializeAsync(IIndexSnapshot<TKey> snapshot, Stream stream, bool disposeStream = true)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            using (var writer = new IndexWriter<TKey>(stream, disposeStream, this.keySerializer))
            {
                await writer.WriteAsync(snapshot).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task SerializeAsync(FullTextIndex<TKey> index, Stream stream, bool disposeStream = true)
        {
            if (index is null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            await this.SerializeAsync(index.Snapshot, stream, disposeStream).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DeserializeAsync(FullTextIndex<TKey> index, Stream stream, bool disposeStream = true)
        {
            if (index is null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            if (index.Count > 0)
            {
                throw new DeserializationException(ExceptionMessages.IndexMustBeEmptyForDeserialization);
            }

            using (var reader = await this.CreateVersionedIndexReaderAsync(stream, disposeStream).ConfigureAwait(false))
            {
                await reader.ReadIntoAsync(index).ConfigureAwait(false);
            }
        }

        private async Task<IIndexReader<TKey>> CreateVersionedIndexReaderAsync(Stream stream, bool disposeStream)
        {
            var version = await ReadFileVersionAsync(stream).ConfigureAwait(false);

            return version switch
            {
                1 => throw new DeserializationException(ExceptionMessages.EarlierVersionSerializedIndexNotSupported, version),
                2 => new V2IndexReader<TKey>(stream, disposeStream, this.keySerializer),
                3 => new V3IndexReader<TKey>(stream, disposeStream, this.keySerializer),
                4 => new V4IndexReader<TKey>(stream, disposeStream, this.keySerializer),
                _ => throw new DeserializationException(ExceptionMessages.NoDeserializerAvailableForIndexVersion, version),
            };
        }

        private static async Task<ushort> ReadFileVersionAsync(Stream stream)
        {
            var data = new byte[4];
            if (await stream.ReadAsync(data, 0, 4).ConfigureAwait(false) != 4)
            {
                throw new DeserializationException(ExceptionMessages.UnableToReadHeaderInformation);
            }

            if (data[0] == 0x4C && data[1] == 0x49)
            {
                return (ushort)((data[3] << 8) + data[2]);
            }

            throw new DeserializationException(ExceptionMessages.MissingLiftiHeaderIndicatorBytes);
        }
    }
}
