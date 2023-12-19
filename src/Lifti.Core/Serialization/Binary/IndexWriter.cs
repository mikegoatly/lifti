using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    internal class IndexWriter<TKey> : IIndexWriter<TKey>
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

        public async Task WriteAsync(IIndexSnapshot<TKey> snapshot)
        {
            await this.WriteHeaderAsync(snapshot).ConfigureAwait(false);

            await this.WriteFieldsAsync(snapshot).ConfigureAwait(false);

            await this.WriteDocumentsAsync(snapshot).ConfigureAwait(false);

            await this.WriteNodeAsync(snapshot.Root).ConfigureAwait(false);

            await this.WriteTerminatorAsync().ConfigureAwait(false);
        }

        private async Task WriteFieldsAsync(IIndexSnapshot<TKey> snapshot)
        {
            // We need to write information for all the fields in the index so that when 
            // we deserialize them to a new index we can ensure that the field ids are
            // mapped correctly to a new index structure as new static fields may be registered
            // in a new version of the index.
            var fieldNames = snapshot.FieldLookup.AllFieldNames;

            this.writer.Write((byte)fieldNames.Count);

            foreach (var fieldName in fieldNames)
            {
                var field = snapshot.FieldLookup.GetFieldInfo(fieldName);
                this.writer.Write(field.Id);
                this.writer.Write((byte)field.FieldKind);
                this.writer.Write(field.Name);

                if (field.FieldKind == FieldKind.Dynamic)
                {
                    if (field.DynamicFieldReaderName == null)
                    {
                        throw new LiftiException(ExceptionMessages.NoDynamicFieldReaderNameInDynamicField);
                    }

                    this.writer.Write(field.DynamicFieldReaderName);
                }
            }

            await this.FlushAsync().ConfigureAwait(false);
        }

        private async Task WriteNodeAsync(IndexNode node)
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
                foreach (var childNode in node.ChildNodes)
                {
                    this.writer.WriteVarUInt16(childNode.Key);
                    await this.WriteNodeAsync(childNode.Value).ConfigureAwait(false);
                }
            }

            if (matchCount > 0)
            {
                this.WriteMatchLocations(node);
            }

            if (childNodeCount > 0)
            {
                await this.FlushAsync().ConfigureAwait(false);
            }
        }

        private void WriteMatchLocations(IndexNode node)
        {
            foreach (var match in node.Matches)
            {
                this.writer.WriteNonNegativeVarInt32(match.Key);
                this.writer.WriteNonNegativeVarInt32(match.Value.Count);

                foreach (var fieldMatch in match.Value)
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

        private async Task WriteTerminatorAsync()
        {
            this.writer.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
            await this.FlushAsync().ConfigureAwait(false);
        }

        private async Task WriteDocumentsAsync(IIndexSnapshot<TKey> index)
        {
            this.writer.WriteNonNegativeVarInt32(index.Items.Count);

            foreach (var itemMetadata in index.Items.GetIndexedItems())
            {
                // Write the standard information for the document, regardless of whether is was
                // read from an object
                this.writer.WriteNonNegativeVarInt32(itemMetadata.Id);
                this.keySerializer.Write(this.writer, itemMetadata.Key);
                this.writer.WriteNonNegativeVarInt32(itemMetadata.DocumentStatistics.TokenCountByField.Count);
                foreach (var fieldTokenCount in itemMetadata.DocumentStatistics.TokenCountByField)
                {
                    this.writer.Write(fieldTokenCount.Key);
                    this.writer.WriteNonNegativeVarInt32(fieldTokenCount.Value);
                }

                // If the object is associated to an object type, write the object type id and any
                // associated freshness info
                if (itemMetadata.ObjectTypeId is byte objectTypeId)
                {
                    this.WriteDocumentObjectMetadata(objectTypeId, itemMetadata);
                }
                else
                {
                    // Write a zero byte to indicate that there is no object type id or metadata
                    this.writer.Write((byte)0);
                }
            }

            await this.FlushAsync().ConfigureAwait(false);
        }

        private void WriteDocumentObjectMetadata(byte objectTypeId, ItemMetadata<TKey> itemMetadata)
        {
            // Write the object info data byte
            // 0-4: The object type id
            // 5: 1 - the object has a scoring freshness date
            // 6: 1 - the object has a scoring magnitude
            // 7: RESERVED for now
            var objectInfoData = objectTypeId;
            Debug.Assert(objectTypeId < 32, "The object type id should be less than 32");

            if (itemMetadata.ScoringFreshnessDate != null)
            {
                objectInfoData |= 0x20;
            }

            if (itemMetadata.ScoringMagnitude != null)
            {
                objectInfoData |= 0x40;
            }

            this.writer.Write(objectInfoData);

            if (itemMetadata.ScoringFreshnessDate is DateTime scoringFreshnessDate)
            {
                this.writer.Write(scoringFreshnessDate.Ticks);
            }

            if (itemMetadata.ScoringMagnitude is double scoringMagnitude)
            {
                this.writer.Write(scoringMagnitude);
            }
        }

        private async Task WriteHeaderAsync(IIndexSnapshot<TKey> index)
        {
            this.writer.Write(new byte[] { 0x4C, 0x49 });
            this.writer.Write(Version);

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
