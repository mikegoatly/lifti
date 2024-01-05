using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    internal class V2IndexReader<TKey> : IIndexDeserializer<TKey>
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

        public async ValueTask ReadAsync(
            FullTextIndex<TKey> index,
            CancellationToken cancellationToken)
        {
            await this.FillBufferAsync().ConfigureAwait(false);

            // If the key serializer derives from KeySerializerBase, use the backwards compatible read method
            // to allow for the old format to be read.
            Func<BinaryReader, TKey> keyReader = this.keySerializer is KeySerializerBase<TKey> baseKeySerializer
                ? baseKeySerializer.ReadV2BackwardsCompatible
                : this.keySerializer.Read;

            // Keep track of all the distinct fields ids encountered during deserialization
            var distinctFieldIds = new HashSet<byte>();

            var documentCount = this.reader.ReadInt32();
            var documentMetadataCollector = new DocumentMetadataCollector<TKey>(documentCount);
            for (var i = 0; i < documentCount; i++)
            {
                var id = this.reader.ReadInt32();
                var key = keyReader(this.reader);
                var fieldStatCount = this.reader.ReadInt32();
                var fieldTokenCounts = new Dictionary<byte, int>(fieldStatCount);
                var totalTokenCount = 0;
                for (var fieldIndex = 0; fieldIndex < fieldStatCount; fieldIndex++)
                {
                    var fieldId = this.reader.ReadByte();
                    distinctFieldIds.Add(fieldId);

                    var wordCount = this.reader.ReadInt32();
                    fieldTokenCounts.Add(fieldId, wordCount);
                    totalTokenCount += wordCount;
                }

                var documentStatistics = new DocumentStatistics(fieldTokenCounts, totalTokenCount);

                // Using ForLooseText here because we don't know any of the new information associated to an object
                // type, e.g. its id or score boost options. This is the closest we can get to the old format.
                documentMetadataCollector.Add(DocumentMetadata.ForLooseText(id, key, documentStatistics));
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

            var rootNode = this.DeserializeNode(index.IndexNodeFactory, 0);

            if (this.reader.ReadInt32() != -1)
            {
                throw new DeserializationException(ExceptionMessages.MissingIndexTerminator);
            }

            if (this.underlyingStream.CanSeek)
            {
                this.underlyingStream.Position = this.buffer.Position + this.initialUnderlyingStreamOffset;
            }

            index.RestoreIndex(rootNode, documentMetadataCollector);
        }

        private IndexNode DeserializeNode(IIndexNodeFactory nodeFactory, int depth)
        {
            var textLength = this.reader.ReadInt32();
            var matchCount = this.reader.ReadInt32();
            var childNodeCount = this.reader.ReadInt32();
            var intraNodeText = textLength == 0 ? null : this.ReadIntraNodeText(textLength);
            var childNodes = childNodeCount > 0 ? new ChildNodeMapEntry[childNodeCount] : null;
            var matches = matchCount > 0 ? new Dictionary<int, IReadOnlyList<IndexedToken>>() : null;

            for (var i = 0; i < childNodeCount; i++)
            {
                var matchChar = this.ReadMatchedCharacter();
                childNodes![i] = new(matchChar, this.DeserializeNode(nodeFactory, depth + 1));
            }

            for (var documentMatch = 0; documentMatch < matchCount; documentMatch++)
            {
                var documentId = this.reader.ReadInt32();
                var fieldCount = this.reader.ReadInt32();
                var indexedTokens = new IndexedToken[fieldCount];

                for (var fieldMatch = 0; fieldMatch < fieldCount; fieldMatch++)
                {
                    var fieldId = this.reader.ReadByte();
                    var locationCount = this.reader.ReadInt32();
                    var locationMatches = new List<TokenLocation>(locationCount);
                    this.ReadLocations(locationCount, locationMatches);

                    indexedTokens[fieldMatch] = new IndexedToken(fieldId, locationMatches);
                }

                matches!.Add(documentId, indexedTokens);
            }

            return nodeFactory.CreateNode(
                intraNodeText,
                childNodes == null ? ChildNodeMap.Empty : new ChildNodeMap(childNodes),
                matches == null ? DocumentTokenMatchMap.Empty : new DocumentTokenMatchMap(matches));
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
                    if (lastLocation is null)
                    {
                        throw new DeserializationException(ExceptionMessages.MalformedDataExpectedFullLocationEntry);
                    }

                    location = this.DeserializeLocationData(lastLocation, structureType);
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
