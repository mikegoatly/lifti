using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Serialization
{
    /// <summary>
    /// A base class helper for implementing <see cref="IIndexSerializer{TKey}"/> implementations.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of key in the index.
    /// </typeparam>
    public abstract class IndexSerializerBase<TKey> : IIndexSerializer<TKey>
    {
        /// <inheritdoc />
        public async ValueTask WriteAsync(IIndexSnapshot<TKey> snapshot, CancellationToken cancellationToken)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            await this.OnSerializationStart(snapshot, cancellationToken).ConfigureAwait(false);

            await this.WriteFieldsAsync(snapshot, cancellationToken).ConfigureAwait(false);

            await this.WriteIndexMetadataAsync(snapshot, cancellationToken).ConfigureAwait(false);

            await this.WriteNodesAsync(snapshot.Root, cancellationToken).ConfigureAwait(false);

            await this.OnSerializationComplete(snapshot, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Called when the serialization of the index is complete. This can be used to write out any footer information
        /// required by the serialization format.
        /// </summary>
        /// <param name="snapshot">
        /// The snapshot of the index that has been serialized.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        protected virtual ValueTask OnSerializationComplete(IIndexSnapshot<TKey> snapshot, CancellationToken cancellationToken)
        {
            return default;
        }

        /// <summary>
        /// Called when the serialization of the index is about to start. This can be used to write out any header information
        /// required by the serialization format.
        /// </summary>
        /// <param name="snapshot">
        /// The snapshot of the index that has been serialized.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        protected virtual ValueTask OnSerializationStart(IIndexSnapshot<TKey> snapshot, CancellationToken cancellationToken)
        {
            return default;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of any resources used by the writer.
        /// </summary>
        /// <param name="disposing">
        /// True if the writer is being disposed of, false if it is being finalized.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Writes the <see cref="SerializedFieldInfo"/> for each field in the index.
        /// </summary>
        /// <param name="fields">
        /// The <see cref="SerializedFieldInfo"/> for each field in the index.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        protected abstract ValueTask WriteFieldsAsync(IReadOnlyList<SerializedFieldInfo> fields, CancellationToken cancellationToken);

        /// <summary>
        /// Writes all the metadata for the index, including document statistics.
        /// </summary>
        /// <param name="index">
        /// The index to write the metadata for.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        protected abstract ValueTask WriteIndexMetadataAsync(IIndexSnapshot<TKey> index, CancellationToken cancellationToken);

        /// <summary>
        /// Writes all the <see cref="IndexNode"/>s for the index, starting from the root node.
        /// </summary>
        /// <param name="rootNode">
        /// The root node to start from. All child nodes must also be written as part of this operation.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        protected abstract ValueTask WriteNodesAsync(IndexNode rootNode, CancellationToken cancellationToken);

        private async ValueTask WriteFieldsAsync(IIndexSnapshot<TKey> snapshot, CancellationToken cancellationToken)
        {
            // We need to write information for all the fields in the index so that when 
            // we deserialize them to a new index we can ensure that the field ids are
            // mapped correctly to a new index structure as new static fields may be registered
            // in a new version of the index.
            var fieldNames = snapshot.FieldLookup.AllFieldNames;
            List<SerializedFieldInfo> fields = new(fieldNames.Count);

            foreach (var fieldName in fieldNames)
            {
                var field = snapshot.FieldLookup.GetFieldInfo(fieldName);
                fields.Add(new SerializedFieldInfo(field.Id, field.Name, field.FieldKind, field.DynamicFieldReaderName));
            }

            await this.WriteFieldsAsync(fields, cancellationToken).ConfigureAwait(false);
        }
    }
}
