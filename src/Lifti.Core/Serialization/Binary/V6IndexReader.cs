using System;
using System.Collections.Immutable;
using System.IO;

namespace Lifti.Serialization.Binary
{
    internal class V6IndexReader<TKey> : V5IndexReader<TKey>, IDisposable
        where TKey : notnull
    {
        public V6IndexReader(Stream stream, bool disposeStream, IKeySerializer<TKey> keySerializer)
            : base(stream, disposeStream, keySerializer)
        {
        }

        /// <remarks>
        /// Version 6 of the index adds in scoring metadata for each document and its associated object type. 
        /// When reading document we now need to additionally read any associated object type id, and the score boost
        /// metadata associated to the object.
        /// </remarks>
        protected override void ReadIndexedDocuments(FullTextIndex<TKey> index)
        {
            var documentCount = this.reader.ReadNonNegativeVarInt32();
            for (var i = 0; i < documentCount; i++)
            {
                // First read the common information that's available whether or not the document is associated to an object
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

                // Now read the object type information, if any
                var objectBitMaskInfo = this.reader.ReadByte();
                if (objectBitMaskInfo != 0)
                {
                    // The object bit mask is:
                    // 0-4: The object type id
                    // 5: 1 - the object has a scoring freshness date
                    // 6: 1 - the object has a scoring magnitude
                    // 7: RESERVED for now
                    var objectTypeId = (byte)(objectBitMaskInfo & 0x1F);
                    var hasScoringFreshnessDate = (objectBitMaskInfo & 0x20) != 0;
                    var hasScoringMagnitude = (objectBitMaskInfo & 0x40) != 0;

                    DateTime? freshnessDate = hasScoringFreshnessDate ? new DateTime(this.reader.ReadInt64()) : null;
                    double? magnitude = hasScoringMagnitude ? this.reader.ReadDouble() : null;

                    index.Items.Add(ItemMetadata<TKey>.ForObject(objectTypeId, id, key, documentStatistics, freshnessDate, magnitude));
                }
                else
                {
                    index.Items.Add(ItemMetadata<TKey>.ForLooseText(id, key, documentStatistics));
                }
            }
        }
    }
}
