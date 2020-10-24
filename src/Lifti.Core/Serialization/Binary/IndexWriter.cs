using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    internal class IndexWriter<TKey> : IIndexWriter<TKey>
    {
        private const ushort Version = 3;
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

            await this.WriteItemsAsync(snapshot).ConfigureAwait(false);

            await this.WriteNodeAsync(snapshot.Root).ConfigureAwait(false);

            await this.WriteTerminatorAsync().ConfigureAwait(false);
        }

        private async Task WriteNodeAsync(IndexNode node)
        {
            var matchCount = node.Matches.Count;
            var childNodeCount = node.ChildNodes.Count;
            var intraNodeTextLength = node.IntraNodeText.Length;
            this.writer.Write(intraNodeTextLength);
            this.writer.Write(matchCount);
            this.writer.Write(childNodeCount);

            if (intraNodeTextLength > 0)
            {
                static void WriteIntraNodeText(BinaryWriter writer, ReadOnlySpan<char> span)
                {
                    for (var i = 0; i < span.Length; i++)
                    {
                        writer.Write(span[i]);
                    }
                }

                WriteIntraNodeText(this.writer, node.IntraNodeText.Span);
            }

            if (childNodeCount > 0)
            {
                foreach (var childNode in node.ChildNodes)
                {
                    this.writer.Write((short)childNode.Key);
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
                this.writer.Write(match.Key);
                this.writer.Write(match.Value.Count);

                foreach (var fieldMatch in match.Value)
                {
                    this.writer.Write(fieldMatch.FieldId);
                    this.writer.Write(fieldMatch.Locations.Count);
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
                this.writer.Write(length);
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
            this.writer.Write(location.TokenIndex);
            this.writer.Write(location.Start);
            this.writer.Write(location.Length);
        }

        private async Task WriteTerminatorAsync()
        {
            this.writer.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
            await this.FlushAsync().ConfigureAwait(false);
        }

        private async Task WriteItemsAsync(IIndexSnapshot<TKey> index)
        {
            foreach (var itemMetadata in index.Items.GetIndexedItems())
            {
                this.writer.Write(itemMetadata.Id);
                this.keySerializer.Write(this.writer, itemMetadata.Item);
                this.writer.Write(itemMetadata.DocumentStatistics.TokenCountByField.Count);
                foreach (var fieldTokenCount in itemMetadata.DocumentStatistics.TokenCountByField)
                {
                    this.writer.Write(fieldTokenCount.Key);
                    this.writer.Write(fieldTokenCount.Value);
                }
            }

            await this.FlushAsync().ConfigureAwait(false);
        }

        private async Task WriteHeaderAsync(IIndexSnapshot<TKey> index)
        {
            this.writer.Write(new byte[] { 0x4C, 0x49 });
            this.writer.Write(Version);
            this.writer.Write(index.Items.Count);

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
