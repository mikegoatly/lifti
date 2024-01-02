using Lifti.Tokenization.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    /// <inheritdoc />
    public sealed class IndexMetadata<TKey> : IIndexMetadata<TKey>
        where TKey : notnull
    {
        private readonly Dictionary<byte, ScoreBoostMetadata> scoreBoostMetadata;
        private readonly IdPool<TKey> idPool;

        internal IndexMetadata(IEnumerable<IObjectTypeConfiguration> configureObjectTypes)
        {
            this.idPool = new IdPool<TKey>();
            this.DocumentKeyLookup = [];
            this.DocumentIdLookup = [];
            this.IndexStatistics = new();
            this.scoreBoostMetadata = configureObjectTypes.ToDictionary(o => o.Id, o => new ScoreBoostMetadata(o.ScoreBoostOptions));
        }

        /// <summary>
        /// Creates a new <see cref="IndexMetadata{TKey}"/> instance that is a copy of the given instance and is safe to mutate.
        /// </summary>
        /// <param name="original"></param>
        internal IndexMetadata(IndexMetadata<TKey> original)
        {
            this.idPool = original.idPool;
            this.DocumentKeyLookup = new(original.DocumentKeyLookup);
            this.DocumentIdLookup = new(original.DocumentIdLookup);
            this.IndexStatistics = new(original.IndexStatistics);
            this.scoreBoostMetadata = original.scoreBoostMetadata;
        }

        /// <inheritdoc />
        [Obsolete("Use DocumentCount property instead")]
        public int Count => this.DocumentCount;

        /// <inheritdoc />
        public int DocumentCount => this.DocumentKeyLookup.Count;

        /// <inheritdoc />
        public IndexStatistics IndexStatistics { get; }

        /// <summary>
        /// Gets or sets the lookup of document key to <see cref="DocumentMetadata{T}"/> information.
        /// </summary>
        private Dictionary<TKey, DocumentMetadata<TKey>> DocumentKeyLookup { get; set; }

        /// <summary>
        /// Gets or sets the lookup of internal document id to <see cref="DocumentMetadata{T}"/> information.
        /// </summary>
        private Dictionary<int, DocumentMetadata<TKey>> DocumentIdLookup { get; set; }

        /// <inheritdoc />\
        public IEnumerable<DocumentMetadata<TKey>> GetIndexedDocuments()
        {
            return this.DocumentKeyLookup.Values;
        }

        /// <summary>
        /// Adds the given document key and associated <see cref="DocumentStatistics"/>. Used when indexing loose text not associated with an object.
        /// </summary>
        /// <param name="key">
        /// The key to add.
        /// </param>
        /// <param name="documentStatistics">
        /// The document statistics for the document.
        /// </param>
        /// <returns>
        /// The internal document id.
        /// </returns>
        public int Add(TKey key, DocumentStatistics documentStatistics)
        {
            return this.Add(
                id => DocumentMetadata.ForLooseText(id, key, documentStatistics));
        }

        /// <inheritdoc />
        public void Add(DocumentMetadata<TKey> documentMetadata)
        {
            if (documentMetadata is null)
            {
                throw new ArgumentNullException(nameof(documentMetadata));
            }

            // Make the ID pool aware of the ID we are using
            this.idPool.RegisterUsedId(documentMetadata.Id);

            if (documentMetadata.ObjectTypeId is byte objectTypeId)
            {
                // Add the document to the overall score boost metadata for the object type
                this.GetObjectTypeScoreBoostMetadata(objectTypeId)
                    .Add(documentMetadata);
            }

            this.UpdateLookups(documentMetadata);
        }

        /// <inheritdoc />
        public DocumentMetadata<TKey> GetMetadata(int documentId)
        {
            if (!this.DocumentIdLookup.TryGetValue(documentId, out var documentMetadata))
            {
                throw new LiftiException(ExceptionMessages.ItemNotFound);
            }

            return documentMetadata;
        }

        /// <inheritdoc />
        public DocumentMetadata<TKey> GetMetadata(TKey key)
        {
            if (!this.DocumentKeyLookup.TryGetValue(key, out var documentMetadata))
            {
                throw new LiftiException(ExceptionMessages.ItemNotFound);
            }

            return documentMetadata;
        }

        /// <inheritdoc />
        public bool Contains(TKey key)
        {
            return this.DocumentKeyLookup.ContainsKey(key);
        }

        /// <inheritdoc />
        public ScoreBoostMetadata GetObjectTypeScoreBoostMetadata(byte objectTypeId)
        {
            if (!this.scoreBoostMetadata.TryGetValue(objectTypeId, out var scoreBoostMetadata))
            {
                throw new LiftiException(ExceptionMessages.UnknownObjectTypeId, objectTypeId);
            }

            return scoreBoostMetadata;
        }

        /// <inheritdoc />
        DocumentMetadata IIndexMetadata.GetMetadata(int documentId)
        {
            return this.GetMetadata(documentId);
        }

        /// <summary>
        /// Removes information about a document from this instance.
        /// </summary>
        /// <returns>
        /// The internal document id of the removed document.
        /// </returns>
        internal int Remove(TKey key)
        {
            var documentInfo = this.DocumentKeyLookup[key];
            var documentId = documentInfo.Id;
            this.DocumentKeyLookup.Remove(key);
            this.DocumentIdLookup.Remove(documentId);
            this.IndexStatistics.Remove(documentInfo.DocumentStatistics);

            if (documentInfo.ObjectTypeId is byte objectTypeId)
            {
                // Remove the document from the overall score boost metadata for the object type
                this.GetObjectTypeScoreBoostMetadata(objectTypeId)
                    .Remove(documentInfo);
            }

            this.idPool.Return(documentId);

            return documentId;
        }

        /// <summary>
        /// Adds the given document key associated to the given object.
        /// </summary>
        /// <inheritdoc cref="Add(TKey, DocumentStatistics)" />
        internal int Add<TObject>(TKey key, TObject item, DocumentStatistics documentStatistics, ObjectTypeConfiguration<TObject, TKey> objectConfiguration)
        {
            // Get the score boosts for the item
            var scoreBoostOptions = objectConfiguration.ScoreBoostOptions;
            var freshnessDate = scoreBoostOptions.FreshnessProvider?.Invoke(item);
            var scoringMagnitude = scoreBoostOptions.MagnitudeProvider?.Invoke(item);

            return this.Add(
                documentId =>
                {
                    var documentMetadata = DocumentMetadata.ForObject(
                        objectTypeId: objectConfiguration.Id,
                        documentId: documentId,
                        key,
                        documentStatistics,
                        freshnessDate,
                        scoringMagnitude);

                    this.GetObjectTypeScoreBoostMetadata(objectConfiguration.Id)
                        .Add(documentMetadata);

                    return documentMetadata;
                });
        }

        private int Add(Func<int, DocumentMetadata<TKey>> createDocumentMetadata)
        {
            var documentId = this.idPool.Next();
            var documentMetadata = createDocumentMetadata(documentId);

            this.Add(documentMetadata);

            return documentId;
        }

        private void UpdateLookups(DocumentMetadata<TKey> documentMetadata)
        {
            var key = documentMetadata.Key;
            var documentId = documentMetadata.Id;
            if (this.DocumentKeyLookup.ContainsKey(key))
            {
                throw new LiftiException(ExceptionMessages.ItemAlreadyIndexed);
            }

            if (this.DocumentIdLookup.ContainsKey(documentId))
            {
                throw new LiftiException(ExceptionMessages.IdAlreadyUsed, documentId);
            }

            this.DocumentKeyLookup.Add(key, documentMetadata);
            this.DocumentIdLookup.Add(documentId, documentMetadata);
            this.IndexStatistics.Add(documentMetadata.DocumentStatistics);
        }
    }
}
