using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    internal class IndexWriter<TKey> : IIndexWriter<TKey>
    {
        private const ushort Version = 1;
        private readonly Stream underlyingStream;
        private readonly bool disposeStream;
        private readonly IKeySerializer<TKey> keySerializer;
        private readonly MemoryStream buffer;
        private readonly BinaryWriter writer;

        public IndexWriter(Stream stream, bool disposeStream, IKeySerializer<TKey> keySerializer)
        {
            this.underlyingStream = stream;
            this.disposeStream = disposeStream;
            this.keySerializer = keySerializer;
            this.buffer = new MemoryStream();
            this.writer = new BinaryWriter(this.buffer, Encoding.UTF8);
        }
        
        public async Task WriteAsync(IFullTextIndex<TKey> index)
        {
            await this.WriteHeaderAsync(index).ConfigureAwait(false);

            await this.WriteItemsAsync(index).ConfigureAwait(false);

            await this.WriteNodeAsync(index.Root).ConfigureAwait(false);

            await this.WriteTerminatorAsync().ConfigureAwait(false);
        }

        private async Task WriteNodeAsync(IndexNode node)
        {
            var matchCount = node.Matches?.Count ?? 0;
            var childNodeCount = node.ChildNodes?.Count ?? 0;
            var intraNodeTextLength = node.IntraNodeText?.Length ?? 0;
            this.writer.Write(intraNodeTextLength);
            this.writer.Write(matchCount);
            this.writer.Write(childNodeCount);

            if (intraNodeTextLength > 0)
            {
                this.writer.Write(node.IntraNodeText);
            }

            if (childNodeCount > 0)
            {
                foreach (var childNode in node.ChildNodes)
                {
                    this.writer.Write(childNode.Key);
                    await this.WriteNodeAsync(childNode.Value).ConfigureAwait(false);
                }
            }

            if (matchCount > 0)
            {
                WriteMatchLocations(node);
            }

            await this.FlushAsync().ConfigureAwait(false);
        }

        private void WriteMatchLocations(IndexNode node)
        {
            foreach (var match in node.Matches)
            {
                this.writer.Write(match.Key);
                this.writer.Write(match.Value.Count);

                foreach (var fieldMatch in match.Value)
                {
                    this.writer.Write(fieldMatch.FieldId);
                    this.writer.Write(fieldMatch.Locations.Count);

                    foreach (var location in fieldMatch.Locations)
                    {
                        this.writer.Write(location.WordIndex);
                        this.writer.Write(location.Start);
                        this.writer.Write(location.Length);
                    }
                }
            }
        }

        private async Task WriteTerminatorAsync()
        {
            this.writer.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
            await this.FlushAsync().ConfigureAwait(false);
        }

        private async Task WriteItemsAsync(IFullTextIndex<TKey> index)
        {
            foreach (var (item, itemId) in index.IdPool.GetIndexedItems())
            {
                this.writer.Write(itemId);
                this.keySerializer.Write(this.writer, item);
            }

            await this.FlushAsync().ConfigureAwait(false);
        }

        private async Task WriteHeaderAsync(IFullTextIndex<TKey> index)
        {
            this.writer.Write(new byte[] { 0x4C, 0x49 });
            this.writer.Write(Version);
            this.writer.Write(index.Count);

            await this.FlushAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            this.writer.Dispose();
            this.buffer.Dispose();

            if (this.disposeStream)
            {
                this.underlyingStream.Dispose();
            }
        }

        private async Task FlushAsync()
        {
            this.writer.Flush();
            this.buffer.Position = 0L;
            await this.buffer.CopyToAsync(this.underlyingStream).ConfigureAwait(false);
            this.buffer.SetLength(0L);
        }
    }
}
