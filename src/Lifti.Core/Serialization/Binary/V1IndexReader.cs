using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    internal class V1IndexReader<TKey> : IIndexReader<TKey>
    {
        private Stream underlyingStream;
        private bool disposeStream;
        private IKeySerializer<TKey> keySerializer;
        private MemoryStream buffer;
        private BinaryReader reader;

        public V1IndexReader(Stream stream, bool disposeStream, IKeySerializer<TKey> keySerializer)
        {
            this.underlyingStream = stream;
            this.disposeStream = disposeStream;
            this.keySerializer = keySerializer;

            this.buffer = new MemoryStream((int)underlyingStream.Length);
            this.reader = new BinaryReader(this.buffer);
        }

        public void Dispose()
        {
            this.reader.Dispose();
            this.buffer.Dispose();

            if (this.disposeStream)
            {
                this.underlyingStream.Dispose();
            }
        }

        public async Task ReadIntoAsync(IFullTextIndex<TKey> index)
        {
            await this.FillBufferAsync().ConfigureAwait(false);

            var itemCount = this.reader.ReadInt32();
            for (var i = 0; i < itemCount; i++)
            {
                var id = this.reader.ReadInt32();
                var key = this.keySerializer.Read(this.reader);

                index.IdPool.Add(id, key);
            }

            this.DeserializeNode(index.Root);

            if (this.reader.ReadInt32() != -1)
            {
                throw new DeserializationException(ExceptionMessages.MissingIndexTerminator);
            }
        }

        private void DeserializeNode(IndexNode node)
        {
            var textLength = this.reader.ReadInt32();
            var matchCount = this.reader.ReadInt32();
            var childNodeCount = this.reader.ReadInt32();
            node.IntraNodeText = textLength == 0 ? null : this.reader.ReadChars(textLength);

            for (var i = 0; i < childNodeCount; i++)
            {
                var matchChar = this.reader.ReadChar();
                this.DeserializeNode(node.CreateChildNode(matchChar));
            }

            for (var itemMatch = 0; itemMatch < matchCount; itemMatch++)
            {
                var itemId = this.reader.ReadInt32();
                var fieldCount = this.reader.ReadInt32();

                for (var fieldMatch = 0; fieldMatch < fieldCount; fieldMatch++)
                {
                    var fieldId = this.reader.ReadByte();
                    var locationCount = this.reader.ReadInt32();
                    var locationMatches = new List<WordLocation>(locationCount);
                    for (int locationMatch = 0; locationMatch < locationCount; locationMatch++)
                    {
                        locationMatches.Add(new WordLocation(reader.ReadInt32(), reader.ReadInt32(), reader.ReadUInt16()));
                    }

                    node.AddMatchedItem(itemId, fieldId, locationMatches);
                }
            }
        }

        private async Task FillBufferAsync()
        {
            await this.underlyingStream.CopyToAsync(this.buffer).ConfigureAwait(false);
            this.buffer.Position = 0;
        }
    }
}
