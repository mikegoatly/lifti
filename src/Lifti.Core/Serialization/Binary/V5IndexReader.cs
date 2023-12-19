using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    internal class V5IndexReader<TKey> : IIndexReader<TKey>, IDisposable
        where TKey : notnull
    {
        private readonly Stream underlyingStream;
        private readonly bool disposeStream;
        protected readonly IKeySerializer<TKey> keySerializer;
        private readonly MemoryStream buffer;
        private long initialUnderlyingStreamOffset;
        protected readonly BinaryReader reader;

        public V5IndexReader(Stream stream, bool disposeStream, IKeySerializer<TKey> keySerializer)
        {
            this.disposeStream = disposeStream;
            this.underlyingStream = stream;
            this.keySerializer = keySerializer;

            this.buffer = new MemoryStream((int)(this.underlyingStream.Length - this.underlyingStream.Position));
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

            var fieldIdMap = this.ReadFields(index);
            this.ReadIndexedDocuments(index);

            index.SetRootWithLock(this.DeserializeNode(fieldIdMap, index.IndexNodeFactory, 0));

            if (this.reader.ReadInt32() != -1)
            {
                throw new DeserializationException(ExceptionMessages.MissingIndexTerminator);
            }

            if (this.underlyingStream.CanSeek)
            {
                this.underlyingStream.Position = this.buffer.Position + this.initialUnderlyingStreamOffset;
            }
        }

        private Dictionary<byte, byte> ReadFields(FullTextIndex<TKey> index)
        {
            var fieldCount = this.reader.ReadNonNegativeVarInt32();
            var serializedFields = new List<SerializedFieldInfo>(fieldCount);

            for (var i = 0; i < fieldCount; i++)
            {
                var fieldId = this.reader.ReadByte();
                var kind = (FieldKind)this.reader.ReadByte();
                var name = this.reader.ReadString();
                var dynamicFieldReaderName = kind == FieldKind.Dynamic ? this.reader.ReadString() : null;
                serializedFields.Add(new(fieldId, name, kind, dynamicFieldReaderName));
            }

            return index.RehydrateSerializedFields(serializedFields);
        }

        protected virtual void ReadIndexedDocuments(FullTextIndex<TKey> index)
        {
            var itemCount = this.reader.ReadNonNegativeVarInt32();
            for (var i = 0; i < itemCount; i++)
            {
                var id = this.reader.ReadNonNegativeVarInt32();
                var key = this.keySerializer.Read(this.reader);
                var fieldStatCount = (int)this.reader.ReadByte();
                var fieldTokenCounts = ImmutableDictionary.CreateBuilder<byte, int>();
                var totalTokenCount = 0;
                for (var fieldIndex = 0; fieldIndex < fieldStatCount; fieldIndex++)
                {
                    var fieldId = this.reader.ReadByte();
                    var wordCount = this.reader.ReadNonNegativeVarInt32();
                    fieldTokenCounts.Add(fieldId, wordCount);
                    totalTokenCount += wordCount;
                }

                var documentStatistics = new DocumentStatistics(fieldTokenCounts.ToImmutable(), totalTokenCount);

                // Using ForLooseText here because we don't know any of the new information associated to an object
                // type, e.g. its id or score boost options. This is the closest we can get to the old format.
                index.Items.Add(ItemMetadata<TKey>.ForLooseText(id, key, documentStatistics));
            }
        }

        private IndexNode DeserializeNode(Dictionary<byte, byte> fieldIdMap, IIndexNodeFactory nodeFactory, int depth)
        {
            var textLength = this.reader.ReadNonNegativeVarInt32();
            var matchCount = this.reader.ReadNonNegativeVarInt32();
            var childNodeCount = this.reader.ReadNonNegativeVarInt32();
            var intraNodeText = textLength == 0 ? null : this.ReadIntraNodeText(textLength);
            var childNodes = childNodeCount > 0 ? ImmutableDictionary.CreateBuilder<char, IndexNode>() : null;
            var matches = matchCount > 0 ? ImmutableDictionary.CreateBuilder<int, ImmutableList<IndexedToken>>() : null;

            for (var i = 0; i < childNodeCount; i++)
            {
                var matchChar = (char)this.reader.ReadVarUInt16();
                childNodes!.Add(matchChar, this.DeserializeNode(fieldIdMap, nodeFactory, depth + 1));
            }

            var locationMatches = new List<TokenLocation>(50);
            for (var itemMatch = 0; itemMatch < matchCount; itemMatch++)
            {
                var itemId = this.reader.ReadNonNegativeVarInt32();
                var fieldCount = this.reader.ReadNonNegativeVarInt32();

                var indexedTokens = ImmutableList.CreateBuilder<IndexedToken>();

                for (var fieldMatch = 0; fieldMatch < fieldCount; fieldMatch++)
                {
                    // We read the serialized file id and use the mapping that the index has given us to
                    // map it to the field id in the new index.
                    var fieldId = fieldIdMap[this.reader.ReadByte()];

                    var locationCount = this.reader.ReadNonNegativeVarInt32();

                    locationMatches.Clear();

                    // Resize the collection immediately if required to prevent multiple resizes during deserialization
                    if (locationMatches.Capacity < locationCount)
                    {
                        locationMatches.Capacity = locationCount;
                    }

                    this.ReadLocations(locationCount, locationMatches);

                    indexedTokens.Add(new IndexedToken(fieldId, locationMatches.ToArray()));
                }

                matches!.Add(itemId, indexedTokens.ToImmutable());
            }

            return nodeFactory.CreateNode(
                intraNodeText,
                childNodes?.ToImmutable() ?? ImmutableDictionary<char, IndexNode>.Empty,
                matches?.ToImmutable() ?? ImmutableDictionary<int, ImmutableList<IndexedToken>>.Empty);
        }

        /// <summary>
        /// Reads the intra-node text from the serialized index.
        /// </summary>
        /// <param name="textLength">
        /// The length of the text to read.
        /// </param>
        /// <returns>
        /// The deserialized intra node 
        /// </returns>
        protected virtual char[] ReadIntraNodeText(int textLength)
        {
            // Read characters serialized as Int16s
            var data = new char[textLength];
            for (var i = 0; i < textLength; i++)
            {
                data[i] = (char)this.reader.ReadVarUInt16();
            }

            return data;
        }

        private void ReadLocations(int locationCount, List<TokenLocation> locationMatches)
        {
            TokenLocation? lastLocation = null;
            for (var locationMatch = 0; locationMatch < locationCount; locationMatch++)
            {
                var structureType = (LocationEntrySerializationOptimizations)this.reader.ReadByte();
                TokenLocation location;
                if (structureType == LocationEntrySerializationOptimizations.Full)
                {
                    location = new TokenLocation(
                        this.reader.ReadNonNegativeVarInt32(),
                        this.reader.ReadNonNegativeVarInt32(),
                        this.reader.ReadVarUInt16());
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

        private TokenLocation DeserializeLocationData(TokenLocation previous, LocationEntrySerializationOptimizations structureType)
        {
            return new TokenLocation(
                previous.TokenIndex + this.DeserializeAbbreviatedData(
                    structureType,
                    LocationEntrySerializationOptimizations.TokenIndexByte,
                    LocationEntrySerializationOptimizations.TokenIndexUInt16),
                previous.Start + this.DeserializeAbbreviatedData(
                    structureType,
                    LocationEntrySerializationOptimizations.TokenStartByte,
                    LocationEntrySerializationOptimizations.TokenStartUInt16),
                ((structureType & LocationEntrySerializationOptimizations.LengthSameAsLast) == LocationEntrySerializationOptimizations.LengthSameAsLast) ?
                    previous.Length :
                    this.reader.ReadVarUInt16());
        }

        private int DeserializeAbbreviatedData(LocationEntrySerializationOptimizations structureType, LocationEntrySerializationOptimizations byteSize, LocationEntrySerializationOptimizations uint16Size)
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
            this.initialUnderlyingStreamOffset = this.underlyingStream.Position;
            await this.underlyingStream.CopyToAsync(this.buffer).ConfigureAwait(false);
            this.buffer.Position = 0;
        }
    }
}
