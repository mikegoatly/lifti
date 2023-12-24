using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Serialization
{
    /// <summary>
    /// A base class for index readers capable of deserializing an index's information.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of key in the index.
    /// </typeparam>
    public abstract class IndexDeserializerBase<TKey> : IIndexDeserializer<TKey>
        where TKey : notnull
    {
        /// <inheritdoc />
        public async ValueTask ReadAsync(
            FullTextIndex<TKey> index,
            CancellationToken cancellationToken)
        {
            if (index is null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            await this.OnDeserializationStartingAsync(cancellationToken).ConfigureAwait(false);

            // Deserialize any fields that are used in the index
            var serializedFields = await this.DeserializeKnownFieldsAsync(cancellationToken).ConfigureAwait(false);

            // Map the serialized fields to the fields in the index as it is now structured
            // We do this because a code change may have been made to the index's FullTextIndexBuilder definition
            // since the serialized index was created, which means that serialized field ids may need to be mapped
            // to ids.
            var fieldMap = index.MapSerializedFieldIds(serializedFields.Collected);

            // Next deserialize the document metadata
            var documentMetadata = await this.DeserializeDocumentMetadataAsync(cancellationToken).ConfigureAwait(false);

            // Finally deserialize the index node hierarchy
            var rootNode = await this.DeserializeIndexNodeHierarchyAsync(fieldMap, index.IndexNodeFactory, cancellationToken).ConfigureAwait(false);

            // Update the index with the deserialized information
            index.RestoreIndex(rootNode, documentMetadata);

            await this.OnDeserializationCompleteAsync(index, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Invoked when the deserialization of the index is starting.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        protected virtual ValueTask OnDeserializationStartingAsync(CancellationToken cancellationToken)
        {
            return default;
        }

        /// <summary>
        /// Invoked when the deserialization of the index is complete.
        /// </summary>
        /// <param name="index">
        /// The index, containing the deserialized information.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        protected virtual ValueTask OnDeserializationCompleteAsync(FullTextIndex<TKey> index, CancellationToken cancellationToken)
        {
            return default;
        }

        /// <summary>
        /// Deserializes the known fields from the index into a <see cref="SerializedFieldCollector"/>.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        protected abstract ValueTask<SerializedFieldCollector> DeserializeKnownFieldsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Deserializes the document metadata from the index into a <see cref="DocumentMetadataCollector{TKey}"/>.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        protected abstract ValueTask<DocumentMetadataCollector<TKey>> DeserializeDocumentMetadataAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Deserializes the index node hierarchy for the index, returning the root <see cref="IndexNode"/>.
        /// </summary>
        /// <param name="serializedFieldIdMap">
        /// A map of the serialized field ids to the field ids in the index as it is now structured. Use this to map any deserialized field ids
        /// to their new ids.
        /// </param>
        /// <param name="indexNodeFactory">
        /// The index node factory to use to create the index nodes.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        protected abstract ValueTask<IndexNode> DeserializeIndexNodeHierarchyAsync(SerializedFieldIdMap serializedFieldIdMap, IIndexNodeFactory indexNodeFactory, CancellationToken cancellationToken);

        /// <summary>
        /// Disposes of the resources used by the index reader.
        /// </summary>
        /// <param name="disposing">
        /// True if the object is being disposed, false if it is being finalized.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
