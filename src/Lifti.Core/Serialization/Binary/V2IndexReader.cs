using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{

    internal class V2IndexReader<TKey> : IIndexReader<TKey>
        where TKey : notnull
    {
        private readonly Stream underlyingStream;
        private readonly bool disposeStream;
        private readonly IKeySerializer<TKey> keySerializer;
        private readonly MemoryStream buffer;
        private long initialUnderlyingStreamOffset;
        protected readonly BinaryReader reader;

        public V2IndexReader(Stream stream, bool disposeStream, IKeySerializer<TKey> keySerializer)
        {
            this.underlyingStream = stream;
            this.disposeStream = disposeStream;
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

            // If the key serializer derives from KeySerializerBase, use the backwards compatible read method
            // to allow for the old format to be read.
            Func<BinaryReader, TKey> keyReader = this.keySerializer is KeySerializerBase<TKey> baseKeySerializer
                ? baseKeySerializer.ReadV2BackwardsCompatible
                : this.keySerializer.Read;

            // Keep track of all the distinct fields ids encountered during deserialization
            var distinctFieldIds = new HashSet<byte>();

            var itemCount = this.reader.ReadInt32();
            for (var i = 0; i < itemCount; i++)
            {
                var id = this.reader.ReadInt32();
                var key = keyReader(this.reader);
                var fieldStatCount = this.reader.ReadInt32();
                var fieldTokenCounts = ImmutableDictionary.CreateBuilder<byte, int>();
                var totalTokenCount = 0;
                for (var fieldIndex = 0; fieldIndex < fieldStatCount; fieldIndex++)
                {
                    var fieldId = this.reader.ReadByte();
                    distinctFieldIds.Add(fieldId);

                    var wordCount = this.reader.ReadInt32();
                    fieldTokenCounts.Add(fieldId, wordCount);
                    totalTokenCount += wordCount;
                }

                var documentStatistics = new DocumentStatistics(fieldTokenCounts.ToImmutable(), totalTokenCount);

                index.Items.Add(new ItemMetadata<TKey>(id, key, documentStatistics, null, null));
            }

            // Double check that the index structure is aware of all the fields that are being deserialized
            // We remove field 0 because it's the default field that loose text is associated to, and does
            // not contribute to the total number of named fields.
            distinctFieldIds.Remove(0);
            var indexFields = index.FieldLookup.AllFieldNames.Select(x => index.FieldLookup.GetFieldInfo(x).Id);
            if (distinctFieldIds.Except(indexFields).Any())
            {
                throw new LiftiException(ExceptionMessages.UnknownFieldsInSerializedIndex);
            }

            index.SetRootWithLock(this.DeserializeNode(index.IndexNodeFactory, 0));

            if (this.reader.ReadInt32() != -1)
            {
                throw new DeserializationException(ExceptionMessages.MissingIndexTerminator);
            }

            if (this.underlyingStream.CanSeek)
            {
                this.underlyingStream.Position = this.buffer.Position + this.initialUnderlyingStreamOffset;
            }
        }

        private IndexNode DeserializeNode(IIndexNodeFactory nodeFactory, int depth)
        {
            var textLength = this.reader.ReadInt32();
            var matchCount = this.reader.ReadInt32();
            var childNodeCount = this.reader.ReadInt32();
            var intraNodeText = textLength == 0 ? null : this.ReadIntraNodeText(textLength);
            var childNodes = childNodeCount > 0 ? ImmutableDictionary.CreateBuilder<char, IndexNode>() : null;
            var matches = matchCount > 0 ? ImmutableDictionary.CreateBuilder<int, ImmutableList<IndexedToken>>() : null;

            for (var i = 0; i < childNodeCount; i++)
            {
                var matchChar = this.ReadMatchedCharacter();
                childNodes!.Add(matchChar, this.DeserializeNode(nodeFactory, depth + 1));
            }

            var locationMatches = new List<TokenLocation>(50);
            for (var itemMatch = 0; itemMatch < matchCount; itemMatch++)
            {
                var itemId = this.reader.ReadInt32();
                var fieldCount = this.reader.ReadInt32();

                var indexedTokens = ImmutableList.CreateBuilder<IndexedToken>();

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
            return this.reader.ReadChars(textLength);
        }

        /// <summary>
        /// Reads a character matched at a node.
        /// </summary>
        protected virtual char ReadMatchedCharacter()
        {
            return this.reader.ReadChar();
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
                    location = new TokenLocation(this.reader.ReadInt32(), this.reader.ReadInt32(), this.reader.ReadUInt16());
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
                    this.reader.ReadUInt16());
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
