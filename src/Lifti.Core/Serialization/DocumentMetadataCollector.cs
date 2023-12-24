using System.Collections.Generic;

namespace Lifti.Serialization
{
    /// <summary>
    /// Collects document metadata during a deserialization operation.
    /// </summary>
    public sealed class DocumentMetadataCollector<TKey> : DeserializedDataCollector<DocumentMetadata<TKey>>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DocumentMetadataCollector{TKey}"/> class.
        /// </summary>
        public DocumentMetadataCollector()
            : base(10)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DocumentMetadataCollector{TKey}"/> class.
        /// </summary>
        /// <param name="expectedCount">
        /// The expected number of <see cref="DocumentMetadata{TKey}"/> records to be collected.
        /// </param>
        public DocumentMetadataCollector(int expectedCount)
            : base(expectedCount)
        {
        }
    }
}
