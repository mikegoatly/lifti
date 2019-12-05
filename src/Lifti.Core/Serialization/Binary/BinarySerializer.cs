using System;
using System.IO;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    public class BinarySerializer<TKey> : IIndexSerializer<TKey>
    {
        private readonly IKeySerializer<TKey> keySerializer;

        public BinarySerializer()
            : this(StandardKeySerializerFactory.Create<TKey>())
        {
        }

        public BinarySerializer(IKeySerializer<TKey> keySerializer)
        {
            this.keySerializer = keySerializer;
        }

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

        public async Task SerializeAsync(FullTextIndex<TKey> index, Stream stream, bool disposeStream = true)
        {
            if (index is null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            await this.SerializeAsync(index.Snapshot, stream, disposeStream).ConfigureAwait(false);
        }

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

            switch (version)
            {
                case 1:
                    return new V1IndexReader<TKey>(stream, disposeStream, this.keySerializer);
                default:
                    throw new DeserializationException(ExceptionMessages.NoDeserializerAvailableForIndexVersion, version);
            }
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
