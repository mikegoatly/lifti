using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    internal class V5IndexReader<TKey> : IndexDeserializerBase<TKey>
        where TKey : notnull
    {
        private readonly Stream underlyingStream;
        private readonly bool disposeStream;
        protected readonly IKeySerializer<TKey> keySerializer;
        private readonly MemoryStream buffer;
        private long initialUnderlyingStreamOffset;
        protected readonly BinaryReader reader;
        private Dictionary<(int documentId, byte fieldId), int> maxTokenIndices;

        public V5IndexReader(Stream stream, bool disposeStream, IKeySerializer<TKey> keySerializer)
        {
            this.disposeStream = disposeStream;
            this.underlyingStream = stream;
            this.keySerializer = keySerializer;

            this.buffer = new MemoryStream((int)(this.underlyingStream.Length - this.underlyingStream.Position));
            this.reader = new BinaryReader(this.buffer);
            this.maxTokenIndices = [];
        }

        /// <summary>
        /// Gets or sets a value indicating whether LastTokenIndex values should be inferred from the index
        /// structure during deserialization. Versions prior to V7 do not store LastTokenIndex, so it must be inferred.
        /// </summary>
        protected bool ShouldInferLastTokenIndices { get; init; } = true;

        protected override void UpdateDocumentMetadata(DocumentMetadataCollector<TKey> documentMetadata)
        {
            if (this.ShouldInferLastTokenIndices)
            {
                // Update document metadata with inferred LastTokenIndex values
                this.UpdateDocumentMetadataWithDerivedLastTokenIndices(documentMetadata);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                this.reader.Dispose();
                this.buffer.Dispose();

                if (this.disposeStream)
                {
                    this.underlyingStream.Dispose();
                }
            }
        }

        protected override async ValueTask OnDeserializationStartingAsync(CancellationToken cancellationToken)
        {
            await this.FillBufferAsync().ConfigureAwait(false);
        }

        protected override ValueTask OnDeserializationCompleteAsync(FullTextIndex<TKey> index, CancellationToken cancellationToken)
        {
            if (this.reader.ReadInt32() != -1)
            {
                throw new DeserializationException(ExceptionMessages.MissingIndexTerminator);
            }

            if (this.underlyingStream.CanSeek)
            {
                this.underlyingStream.Position = this.buffer.Position + this.initialUnderlyingStreamOffset;
            }

            return default;
        }

        protected override ValueTask<SerializedFieldCollector> DeserializeKnownFieldsAsync(CancellationToken cancellationToken)
        {
            var fieldCount = this.reader.ReadNonNegativeVarInt32();
            var serializedFields = new SerializedFieldCollector(fieldCount);

            for (var i = 0; i < fieldCount; i++)
            {
                var fieldId = this.reader.ReadByte();
                var kind = (FieldKind)this.reader.ReadByte();
                var name = this.reader.ReadString();
                var dynamicFieldReaderName = kind == FieldKind.Dynamic ? this.reader.ReadString() : null;
                serializedFields.Add(new(fieldId, name, kind, dynamicFieldReaderName));
            }

            return new(serializedFields);
        }

        protected override ValueTask<DocumentMetadataCollector<TKey>> DeserializeDocumentMetadataAsync(CancellationToken cancellationToken)
        {
            var documentCount = this.reader.ReadNonNegativeVarInt32();
            var documentMetadataCollector = new DocumentMetadataCollector<TKey>(documentCount);

            for (var i = 0; i < documentCount; i++)
            {
                var id = this.reader.ReadNonNegativeVarInt32();
                var key = this.keySerializer.Read(this.reader);
                var fieldStatCount = (int)this.reader.ReadByte();
                var fieldTokenCounts = new Dictionary<byte, int>(fieldStatCount);
                var totalTokenCount = 0;
                for (var fieldIndex = 0; fieldIndex < fieldStatCount; fieldIndex++)
                {
                    var fieldId = this.reader.ReadByte();
                    var wordCount = this.reader.ReadNonNegativeVarInt32();
                    fieldTokenCounts.Add(fieldId, wordCount);
                    totalTokenCount += wordCount;
                }

                // Convert to unified FieldStatistics format (V5 doesn't have LastTokenIndex, so use -1)
                var statisticsByField = new Dictionary<byte, FieldStatistics>(fieldTokenCounts.Count);
                foreach (var (fieldId, tokenCount) in fieldTokenCounts)
                {
                    statisticsByField.Add(fieldId, new FieldStatistics(tokenCount, -1));
                }

                var documentStatistics = new DocumentStatistics(statisticsByField, totalTokenCount);

                // Using ForLooseText here because we don't know any of the new information associated to an object
                // type, e.g. its id or score boost options. This is the closest we can get to the old format.
                documentMetadataCollector.Add(DocumentMetadata.ForLooseText(id, key, documentStatistics));
            }

            return new(documentMetadataCollector);
        }

        protected override ValueTask<IndexNode> DeserializeIndexNodeHierarchyAsync(
            SerializedFieldIdMap serializedFieldIdMap,
            IIndexNodeFactory indexNodeFactory,
            CancellationToken cancellationToken)
        {
            return new(this.DeserializeNode(serializedFieldIdMap, indexNodeFactory, 0));
        }

        private IndexNode DeserializeNode(SerializedFieldIdMap fieldIdMap, IIndexNodeFactory nodeFactory, int depth)
        {
            var textLength = this.reader.ReadNonNegativeVarInt32();
            var matchCount = this.reader.ReadNonNegativeVarInt32();
            var childNodeCount = this.reader.ReadNonNegativeVarInt32();
            var intraNodeText = textLength == 0 ? null : this.ReadIntraNodeText(textLength);
            var childNodes = childNodeCount > 0 ? new ChildNodeMapEntry[childNodeCount] : null;
            var matches = matchCount > 0 ? new Dictionary<int, IReadOnlyList<IndexedToken>>() : null;

            for (var i = 0; i < childNodeCount; i++)
            {
                var matchChar = (char)this.reader.ReadVarUInt16();
                childNodes![i] = new(matchChar, this.DeserializeNode(fieldIdMap, nodeFactory, depth + 1));
            }

            for (var documentMatch = 0; documentMatch < matchCount; documentMatch++)
            {
                var documentId = this.reader.ReadNonNegativeVarInt32();
                var fieldCount = this.reader.ReadNonNegativeVarInt32();
                var indexedTokens = new IndexedToken[fieldCount];

                for (var fieldMatch = 0; fieldMatch < fieldCount; fieldMatch++)
                {
                    // We read the serialized file id and use the mapping that the index has given us to
                    // map it to the field id in the new index.
                    var fieldId = fieldIdMap.Map(this.reader.ReadByte());

                    var locationCount = this.reader.ReadNonNegativeVarInt32();
                    var locationMatches = new List<TokenLocation>(locationCount);
                    this.ReadLocations(locationCount, locationMatches);

                    indexedTokens[fieldMatch] = new IndexedToken(fieldId, [.. locationMatches]);

                    if (this.ShouldInferLastTokenIndices)
                    {
                        // Track the maximum token index for this (documentId, fieldId) pair
                        this.CalculateDerivedMaxTokenIndex(documentId, fieldId, locationMatches);
                    }
                }

                matches!.Add(documentId, indexedTokens);
            }

            return nodeFactory.CreateNode(
                intraNodeText,
                childNodes == null ? ChildNodeMap.Empty : new ChildNodeMap(childNodes),
                matches == null ? DocumentTokenMatchMap.Empty : new DocumentTokenMatchMap(matches));
        }

        protected virtual void CalculateDerivedMaxTokenIndex(int documentId, byte fieldId, List<TokenLocation> locationMatches)
        {
            if (locationMatches.Count > 0)
            {
                var maxTokenIndexInThisMatch = locationMatches.Max(loc => loc.TokenIndex);
                var key = (documentId, fieldId);
                if (!this.maxTokenIndices.TryGetValue(key, out var currentMax) || maxTokenIndexInThisMatch > currentMax)
                {
                    this.maxTokenIndices[key] = maxTokenIndexInThisMatch;
                }
            }
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

        /// <summary>
        /// Updates the document metadata with inferred LastTokenIndex values from the tracked max token indices.
        /// V5 format doesn't store LastTokenIndex, so we infer it from the index structure during deserialization.
        /// </summary>
        private void UpdateDocumentMetadataWithDerivedLastTokenIndices(DocumentMetadataCollector<TKey> documentMetadataCollector)
        {
            for (var i = 0; i < documentMetadataCollector.Collected.Count; i++)
            {
                var existingMetadata = documentMetadataCollector.Collected[i];
                var existingStats = existingMetadata.DocumentStatistics;

                // Build updated FieldStatistics with inferred LastTokenIndex values
                var updatedStatsByField = new Dictionary<byte, FieldStatistics>();
                foreach (var (fieldId, fieldStats) in existingStats.StatisticsByField)
                {
                    var key = (existingMetadata.Id, fieldId);
                    var lastTokenIndex = this.maxTokenIndices.TryGetValue(key, out var maxIndex) ? maxIndex : -1;
                    updatedStatsByField[fieldId] = new FieldStatistics(fieldStats.TokenCount, lastTokenIndex);
                }

                // Create new DocumentStatistics with updated field statistics
                var updatedDocStats = new DocumentStatistics(updatedStatsByField, existingStats.TotalTokenCount);

                // Recreate the DocumentMetadata with updated statistics, preserving object metadata if present
                DocumentMetadata<TKey> updatedMetadata;
                if (existingMetadata.ObjectTypeId.HasValue)
                {
                    updatedMetadata = DocumentMetadata.ForObject(
                        existingMetadata.ObjectTypeId.Value,
                        existingMetadata.Id,
                        existingMetadata.Key,
                        updatedDocStats,
                        existingMetadata.ScoringFreshnessDate,
                        existingMetadata.ScoringMagnitude);
                }
                else
                {
                    updatedMetadata = DocumentMetadata.ForLooseText(
                        existingMetadata.Id,
                        existingMetadata.Key,
                        updatedDocStats);
                }

                // Replace in collection
                documentMetadataCollector.Collected[i] = updatedMetadata;
            }
        }
    }
}
