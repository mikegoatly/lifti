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

        public async Task SerializeAsync(IFullTextIndex<TKey> index, Stream stream, bool disposeStream = true)
        {
            if (index is null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            using (var writer = new IndexWriter<TKey>(stream, disposeStream, this.keySerializer))
            {
                await writer.WriteAsync(index).ConfigureAwait(false);
            }
        }
    }
}
