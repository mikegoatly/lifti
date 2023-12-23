using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    internal class IndexWriter<TKey> : IndexSerializerBase<TKey>
    {
        private const ushort Version = 6;
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
            this.buffer = new MemoryStream(32768);
            this.writer = new BinaryWriter(this.buffer, Encoding.UTF8);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                this.writer.Dispose();
                this.buffer.Dispose();

                if (this.disposeStream)
                {
                    this.underlyingStream.Dispose();
                }
            }
        }

        protected override ValueTask OnSerializationStart(IIndexSnapshot<TKey> snapshot, CancellationToken cancellationToken)
        {
            return this.WriteHeaderAsync(cancellationToken);
        }

        protected override async ValueTask WriteFieldsAsync(IReadOnlyList<SerializedFieldInfo> fields, CancellationToken cancellationToken)
        {
            this.writer.Write((byte)fields.Count);

            foreach (var field in fields)
            {
                this.writer.Write(field.FieldId);
                this.writer.Write((byte)field.Kind);
                this.writer.Write(field.Name);

                if (field.Kind == FieldKind.Dynamic)
                {
                    if (field.DynamicFieldReaderName == null)
                    {
                        throw new LiftiException(ExceptionMessages.NoDynamicFieldReaderNameInDynamicField);
                    }

                    this.writer.Write(field.DynamicFieldReaderName);
                }
            }

            await this.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override ValueTask OnSerializationComplete(IIndexSnapshot<TKey> snapshot, CancellationToken cancellationToken)
        {
            return this.WriteTerminatorAsync(cancellationToken);
        }

        protected override async ValueTask WriteIndexMetadataAsync(IIndexSnapshot<TKey> index, CancellationToken cancellationToken)
        {
            this.writer.WriteNonNegativeVarInt32(index.Metadata.Count);

            foreach (var documentMetadata in index.Metadata.GetIndexedDocuments())
            {
                // Write the standard information for the document, regardless of whether is was
                // read from an object
                this.writer.WriteNonNegativeVarInt32(documentMetadata.Id);
                this.keySerializer.Write(this.writer, documentMetadata.Key);
                this.writer.WriteNonNegativeVarInt32(documentMetadata.DocumentStatistics.TokenCountByField.Count);
                foreach (var fieldTokenCount in documentMetadata.DocumentStatistics.TokenCountByField)
                {
                    this.writer.Write(fieldTokenCount.Key);
                    this.writer.WriteNonNegativeVarInt32(fieldTokenCount.Value);
                }

                // If the object is associated to an object type, write the object type id and any
                // associated freshness info
                if (documentMetadata.ObjectTypeId is byte objectTypeId)
                {
                    this.WriteDocumentObjectMetadata(objectTypeId, documentMetadata);
                }
                else
                {
                    // Write a zero byte to indicate that there is no object type id or metadata
                    this.writer.Write((byte)0);
                }
            }

            await this.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override async ValueTask WriteNodesAsync(IndexNode rootNode, CancellationToken cancellationToken)
        {
            await this.WriteNodeAsync(rootNode, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask WriteNodeAsync(IndexNode node, CancellationToken cancellationToken)
        {
            var matchCount = node.Matches.Count;
            var childNodeCount = node.ChildNodes.Count;
            var intraNodeTextLength = node.IntraNodeText.Length;
            this.writer.WriteNonNegativeVarInt32(intraNodeTextLength);
            this.writer.WriteNonNegativeVarInt32(matchCount);
            this.writer.WriteNonNegativeVarInt32(childNodeCount);

            if (intraNodeTextLength > 0)
            {
                this.writer.WriteSpanAsVarInt16s(node.IntraNodeText.Span);
            }

            if (childNodeCount > 0)
            {
                foreach (var (character, childNode) in node.ChildNodes.CharacterMap)
                {
                    this.writer.WriteVarUInt16(character);
                    await this.WriteNodeAsync(childNode, cancellationToken).ConfigureAwait(false);
                }
            }

            if (matchCount > 0)
            {
                this.WriteMatchLocations(node);
            }

            if (childNodeCount > 0)
            {
                await this.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private void WriteMatchLocations(IndexNode node)
        {
            foreach (var (documentId, matches) in node.Matches.Enumerate())
            {
                this.writer.WriteNonNegativeVarInt32(documentId);
                this.writer.WriteNonNegativeVarInt32(matches.Count);

                foreach (var fieldMatch in matches)
                {
                    this.writer.Write(fieldMatch.FieldId);
                    this.writer.WriteNonNegativeVarInt32(fieldMatch.Locations.Count);
                    this.WriteTokenLocations(fieldMatch);
                }
            }
        }

        private void WriteTokenLocations(IndexedToken fieldMatch)
        {
            TokenLocation? lastLocation = null;
            foreach (var location in fieldMatch.Locations)
            {
                if (lastLocation != null)
                {
                    var locationData = DeriveEntryStructureInformation(lastLocation.Value, location);

                    if (locationData.structure == LocationEntrySerializationOptimizations.Full)
                    {
                        this.WriteLocationInFull(location);
                    }
                    else
                    {
                        this.WriteAbbreviatedLocationDetails(location.Length, locationData);
                    }
                }
                else
                {
                    this.WriteLocationInFull(location);
                }

                lastLocation = location;
            }
        }

        private static (LocationEntrySerializationOptimizations structure, int tokenIndexValue, int startValue) DeriveEntryStructureInformation(TokenLocation lastLocation, TokenLocation location)
        {
            var relativeTokenIndex = location.TokenIndex - lastLocation.TokenIndex;
            var relativeStart = location.Start - lastLocation.Start;

            if (relativeTokenIndex < 0 || relativeStart < 0)
            {
                Debug.Fail("Warning: This shouldn't happen");
                return (LocationEntrySerializationOptimizations.Full, location.TokenIndex, location.Start);
            }

            var entryStructure = LocationEntrySerializationOptimizations.Full;
            if (relativeTokenIndex <= byte.MaxValue)
            {
                entryStructure |= LocationEntrySerializationOptimizations.TokenIndexByte;
            }
            else if (relativeTokenIndex <= ushort.MaxValue)
            {
                entryStructure |= LocationEntrySerializationOptimizations.TokenIndexUInt16;
            }

            if (relativeStart <= byte.MaxValue)
            {
                entryStructure |= LocationEntrySerializationOptimizations.TokenStartByte;
            }
            else if (relativeStart <= ushort.MaxValue)
            {
                entryStructure |= LocationEntrySerializationOptimizations.TokenStartUInt16;
            }

            if (lastLocation.Length == location.Length)
            {
                entryStructure |= LocationEntrySerializationOptimizations.LengthSameAsLast;
            }

            return (entryStructure, relativeTokenIndex, relativeStart);
        }

        private void WriteAbbreviatedLocationDetails(ushort length, (LocationEntrySerializationOptimizations structure, int tokenIndex, int start) locationData)
        {
            this.writer.Write((byte)locationData.structure);
            this.WriteAbbreviatedData(locationData.tokenIndex, locationData.structure, LocationEntrySerializationOptimizations.TokenIndexByte, LocationEntrySerializationOptimizations.TokenIndexUInt16);
            this.WriteAbbreviatedData(locationData.start, locationData.structure, LocationEntrySerializationOptimizations.TokenStartByte, LocationEntrySerializationOptimizations.TokenStartUInt16);

            if ((locationData.structure & LocationEntrySerializationOptimizations.LengthSameAsLast) != LocationEntrySerializationOptimizations.LengthSameAsLast)
            {
                this.writer.WriteVarUInt16(length);
            }
        }

        private void WriteAbbreviatedData(int data, LocationEntrySerializationOptimizations structure, LocationEntrySerializationOptimizations byteSize, LocationEntrySerializationOptimizations uint16Size)
        {
            if ((structure & byteSize) == byteSize)
            {
                this.writer.Write((byte)data);
            }
            else if ((structure & uint16Size) == uint16Size)
            {
                this.writer.Write((ushort)data);
            }
            else
            {
                this.writer.Write(data);
            }
        }

        private void WriteLocationInFull(TokenLocation location)
        {
            this.writer.Write((byte)LocationEntrySerializationOptimizations.Full);
            this.writer.WriteNonNegativeVarInt32(location.TokenIndex);
            this.writer.WriteNonNegativeVarInt32(location.Start);
            this.writer.WriteVarUInt16(location.Length);
        }

        private async ValueTask WriteTerminatorAsync(CancellationToken cancellationToken)
        {
            this.writer.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
            await this.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private void WriteDocumentObjectMetadata(byte objectTypeId, DocumentMetadata<TKey> documentMetadata)
        {
            // Write the object info data byte
            // 0-4: The object type id
            // 5: 1 - the object has a scoring freshness date
            // 6: 1 - the object has a scoring magnitude
            // 7: RESERVED for now
            var objectInfoData = objectTypeId;
            Debug.Assert(objectTypeId < 32, "The object type id should be less than 32");

            if (documentMetadata.ScoringFreshnessDate != null)
            {
                objectInfoData |= 0x20;
            }

            if (documentMetadata.ScoringMagnitude != null)
            {
                objectInfoData |= 0x40;
            }

            this.writer.Write(objectInfoData);

            if (documentMetadata.ScoringFreshnessDate is DateTime scoringFreshnessDate)
            {
                this.writer.Write(scoringFreshnessDate.Ticks);
            }

            if (documentMetadata.ScoringMagnitude is double scoringMagnitude)
            {
                this.writer.Write(scoringMagnitude);
            }
        }

        private async ValueTask WriteHeaderAsync(CancellationToken cancellationToken)
        {
            this.writer.Write(new byte[] { 0x4C, 0x49 });
            this.writer.Write(Version);

            await this.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask FlushAsync(CancellationToken cancellationToken)
        {
            this.writer.Flush();
            this.buffer.Position = 0L;
#if NETSTANDARD
            // 81920 is taken from DefaultCopyBufferSize of GetCopyBufferSize in Stream.cs
            await this.buffer.CopyToAsync(this.underlyingStream, 81920, cancellationToken).ConfigureAwait(false);
#else
            await this.buffer.CopyToAsync(this.underlyingStream, cancellationToken).ConfigureAwait(false);
#endif
            this.buffer.SetLength(0L);
        }
    }
}
