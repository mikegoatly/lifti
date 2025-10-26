using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Serialization.Binary
{
    internal class V7IndexReader<TKey> : V6IndexReader<TKey>, IDisposable
        where TKey : notnull
    {
        public V7IndexReader(Stream stream, bool disposeStream, IKeySerializer<TKey> keySerializer)
            : base(stream, disposeStream, keySerializer)
        {
            // In V7 format, LastTokenIndex is stored directly in the serialized data,
            // so inference is not needed (unlike earlier versions).
            this.ShouldInferLastTokenIndices = false;
        }

        /// <remarks>
        /// Version 7 adds unified field statistics (token count and last token index together).
        /// </remarks>
        protected override ValueTask<DocumentMetadataCollector<TKey>> DeserializeDocumentMetadataAsync(CancellationToken cancellationToken)
        {
            var documentCount = this.reader.ReadNonNegativeVarInt32();
            var documentMetadataCollector = new DocumentMetadataCollector<TKey>(documentCount);

            for (var i = 0; i < documentCount; i++)
            {
                // Read common document information
                var id = this.reader.ReadNonNegativeVarInt32();
                var key = this.keySerializer.Read(this.reader);

                // Read field statistics (unified in V7)
                var fieldStatCount = (int)this.reader.ReadByte();
                var statisticsByField = new Dictionary<byte, FieldStatistics>(fieldStatCount);
                var totalTokenCount = 0;
                for (var fieldIndex = 0; fieldIndex < fieldStatCount; fieldIndex++)
                {
                    var fieldId = this.reader.ReadByte();
                    var tokenCount = this.reader.ReadNonNegativeVarInt32();
                    var lastTokenIndex = this.reader.ReadNonNegativeVarInt32();

                    statisticsByField.Add(fieldId, new FieldStatistics(tokenCount, lastTokenIndex));
                    totalTokenCount += tokenCount;
                }

                var documentStatistics = new DocumentStatistics(
                    statisticsByField,
                    totalTokenCount);

                this.ReadObjectTypeInformation(documentMetadataCollector, id, key, documentStatistics);
            }

            return new(documentMetadataCollector);
        }

        protected override void UpdateDocumentMetadata(DocumentMetadataCollector<TKey> documentMetadata)
        {
            // V7 index has all the information it needs. This is a no-op.
        }
    }
}
