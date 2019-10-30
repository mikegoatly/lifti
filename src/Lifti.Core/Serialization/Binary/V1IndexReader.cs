using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    internal class V1IndexReader<TKey> : IIndexReader<TKey>
    {
        private readonly Stream underlyingStream;
        private readonly bool disposeStream;
        private readonly IKeySerializer<TKey> keySerializer;
        private readonly MemoryStream buffer;
        private readonly BinaryReader reader;

        public V1IndexReader(Stream stream, bool disposeStream, IKeySerializer<TKey> keySerializer)
        {
            this.underlyingStream = stream;
            this.disposeStream = disposeStream;
            this.keySerializer = keySerializer;

            this.buffer = new MemoryStream((int)this.underlyingStream.Length);
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

        public async Task ReadIntoAsync(FullTextIndex<TKey> index)
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

            var locationMatches = new List<WordLocation>(50);
            for (var itemMatch = 0; itemMatch < matchCount; itemMatch++)
            {
                var itemId = this.reader.ReadInt32();
                var fieldCount = this.reader.ReadInt32();

                for (var fieldMatch = 0; fieldMatch < fieldCount; fieldMatch++)
                {
                    var fieldId = this.reader.ReadByte();
                    var locationCount = this.reader.ReadInt32();

                    locationMatches.Clear();

                    // Resize the collection immediately if required to prevent multiple resizes during deserialization
                    if (locationMatches.Capacity < locationCount)
                    {
                        locationMatches.Capacity = locationCount;
                    }

                    this.ReadLocations(locationCount, locationMatches);

                    node.AddMatchedItem(itemId, fieldId, locationMatches.ToArray());
                }
            }
        }

        private void ReadLocations(int locationCount, List<WordLocation> locationMatches)
        {
            WordLocation? lastLocation = null;
            for (var locationMatch = 0; locationMatch < locationCount; locationMatch++)
            {
                var structureType = (LocationEntryStructure)this.reader.ReadByte();
                WordLocation location;
                if (structureType == LocationEntryStructure.Full)
                {
                    location = new WordLocation(this.reader.ReadInt32(), this.reader.ReadInt32(), this.reader.ReadUInt16());
                }
                else
                {
                    if (lastLocation == null)
                    {
                        throw new DeserializationException(ExceptionMessages.MalformedDataExpectedFullLocationEntry);
                    }

                    location = this.DeserializeLocationData(lastLocation.Value, structureType);
                }

                locationMatches.Add(location);
                lastLocation = location;
            }
        }

        private WordLocation DeserializeLocationData(WordLocation previous, LocationEntryStructure structureType)
        {
            return new WordLocation(
                previous.WordIndex + this.DeserializeAbbreviatedData(
                    structureType,
                    LocationEntryStructure.WordIndexByte,
                    LocationEntryStructure.WordIndexUInt16),
                previous.Start + this.DeserializeAbbreviatedData(
                    structureType,
                    LocationEntryStructure.WordStartByte,
                    LocationEntryStructure.WordStartUInt16),
                ((structureType & LocationEntryStructure.LengthSameAsLast) == LocationEntryStructure.LengthSameAsLast) ?
                    previous.Length :
                    this.reader.ReadUInt16());
        }

        private int DeserializeAbbreviatedData(LocationEntryStructure structureType, LocationEntryStructure byteSize, LocationEntryStructure uint16Size)
        {
            if ((structureType & byteSize) == byteSize)
            {
                return this.reader.ReadByte();
            }
            else if ((structureType & uint16Size) == uint16Size)
            {
                return this.reader.ReadUInt16();
            }

            return this.reader.ReadInt32();
        }

        private async Task FillBufferAsync()
        {
            await this.underlyingStream.CopyToAsync(this.buffer).ConfigureAwait(false);
            this.buffer.Position = 0;
        }
    }
}
