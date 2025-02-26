using Lifti.Querying;
using Lifti.Serialization;
using Lifti.Tokenization;
using Lifti.Tokenization.Objects;
using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti
{
    /// <inheritdoc />
    public class FullTextIndex<TKey> : IFullTextIndex<TKey>, IDisposable
        where TKey : notnull
    {
        private readonly Func<IIndexSnapshot<TKey>, CancellationToken, Task>[]? indexModifiedActions;
        private readonly IndexOptions indexOptions;
        private readonly IIndexNavigatorPool indexNavigatorPool;
        private readonly SemaphoreSlim writeLock = new(1);
        private readonly TimeSpan writeLockTimeout = TimeSpan.FromSeconds(10);
        private readonly IndexedFieldLookup fieldLookup;
        private IndexMetadata<TKey> metadata;
        private bool isDisposed;

        /// <remarks>
        /// currentSnapshot and root are set via the Root property when it is initialized by the constructor
        /// </remarks>
        private IndexSnapshot<TKey> currentSnapshot = null!;
        private IndexNode root = null!;

        private IndexMutation<TKey>? batchMutation;

        internal FullTextIndex(
            IndexOptions indexOptions,
            ObjectTypeConfigurationLookup<TKey> objectTypeConfiguration,
            IndexedFieldLookup fieldLookup,
            IIndexNodeFactory indexNodeFactory,
            IQueryParser queryParser,
            IIndexScorerFactory scorer,
            ITextExtractor defaultTextExtractor,
            IIndexTokenizer defaultTokenizer,
            IThesaurus defaultThesaurus,
            Func<IIndexSnapshot<TKey>, CancellationToken, Task>[]? indexModifiedActions)
        {
            this.indexNavigatorPool = new IndexNavigatorPool(scorer);
            this.metadata = new IndexMetadata<TKey>(objectTypeConfiguration.AllConfigurations);

            this.indexOptions = indexOptions;
            this.ObjectTypeConfiguration = objectTypeConfiguration ?? throw new ArgumentNullException(nameof(objectTypeConfiguration));
            this.IndexNodeFactory = indexNodeFactory ?? throw new ArgumentNullException(nameof(indexNodeFactory));
            this.QueryParser = queryParser ?? throw new ArgumentNullException(nameof(queryParser));
            this.DefaultTextExtractor = defaultTextExtractor;
            this.DefaultTokenizer = defaultTokenizer ?? throw new ArgumentNullException(nameof(defaultTokenizer));
            this.DefaultThesaurus = defaultThesaurus;
            this.indexModifiedActions = indexModifiedActions;
            this.fieldLookup = fieldLookup;

            this.Root = this.IndexNodeFactory.CreateRootNode();
        }

        /// <inheritdoc />
        public IndexNode Root
        {
            get => this.root;
            private set
            {
                this.root = value;
                this.currentSnapshot = new IndexSnapshot<TKey>(
                    this.indexNavigatorPool,
                    this.fieldLookup,
                    value,
                    this.metadata);
            }
        }

        /// <inheritdoc />
        [Obsolete("Use the Metadata property instead")]
        public IIndexMetadata<TKey> Items => this.Metadata;

        /// <inheritdoc />
        public IIndexMetadata<TKey> Metadata => this.metadata;

        /// <inheritdoc />
        public IIndexedFieldLookup FieldLookup => this.fieldLookup;

        /// <inheritdoc />
        public int Count => this.currentSnapshot.Metadata.DocumentCount;

        internal IIndexNodeFactory IndexNodeFactory { get; }

        /// <inheritdoc />
        public IQueryParser QueryParser { get; }

        /// <inheritdoc />
        public IIndexSnapshot<TKey> Snapshot => this.currentSnapshot;

        /// <inheritdoc />
        public IIndexTokenizer DefaultTokenizer { get; }

        /// <inheritdoc />
        public ITextExtractor DefaultTextExtractor { get; }

        /// <inheritdoc />
        public IThesaurus DefaultThesaurus { get; }

        internal ObjectTypeConfigurationLookup<TKey> ObjectTypeConfiguration { get; }

        /// <summary>
        /// Restores the index from a previously serialized state.
        /// </summary>
        /// <param name="rootNode">
        /// The root node of the index.
        /// </param>
        /// <param name="collectedMetadata">
        /// The metadata for the index.
        /// </param>
        internal void RestoreIndex(IndexNode rootNode, DocumentMetadataCollector<TKey> collectedMetadata)
        {
            // Set the root node and metadata in a write lock to ensure that no other operations are happening
            this.PerformWriteLockedAction(() =>
            {
                foreach (var metadata in collectedMetadata.Collected)
                {
                    this.metadata.Add(metadata);
                }

                this.Root = rootNode;
            });
        }

        /// <inheritdoc />
        public IIndexTokenizer GetTokenizerForField(string fieldName)
        {
            return this.FieldLookup.GetFieldInfo(fieldName).Tokenizer;
        }

        /// <inheritdoc />
        public IIndexNavigator CreateNavigator()
        {
            return this.Snapshot.CreateNavigator();
        }

        /// <inheritdoc />
        public void BeginBatchChange()
        {
            this.PerformWriteLockedAction(() =>
            {
                if (this.batchMutation != null)
                {
                    throw new LiftiException(ExceptionMessages.BatchChangeAlreadyStarted);
                }

                this.batchMutation = new IndexMutation<TKey>(this.Root, this.metadata, this.IndexNodeFactory);
            });
        }

        /// <inheritdoc />
        public async Task CommitBatchChangeAsync(CancellationToken cancellationToken = default)
        {
            await this.PerformWriteLockedActionAsync(
                async () =>
                {
                    if (this.batchMutation == null)
                    {
                        throw new LiftiException(ExceptionMessages.NoBatchChangeInProgress);
                    }

                    await this.ApplyMutationsAsync(this.batchMutation, cancellationToken).ConfigureAwait(false);

                    this.batchMutation = null;
                },
                cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task AddAsync(TKey key, IEnumerable<string> text, CancellationToken cancellationToken = default)
        {
            await this.PerformWriteLockedActionAsync(
                async () =>
                {
                    await this.MutateAsync(
                        m =>
                        {
                            var tokens = ExtractDocumentTokens(text.Select(t => t.AsMemory()), this.DefaultTextExtractor, this.DefaultTokenizer, this.DefaultThesaurus);
                            this.AddForDefaultField(m, key, tokens);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
                },
                cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task AddAsync(TKey key, IEnumerable<ReadOnlyMemory<char>> text, CancellationToken cancellationToken = default)
        {
            await this.PerformWriteLockedActionAsync(
                async () =>
                {
                    await this.MutateAsync(
                        m =>
                        {
                            var tokens = ExtractDocumentTokens(text, this.DefaultTextExtractor, this.DefaultTokenizer, this.DefaultThesaurus);
                            this.AddForDefaultField(m, key, tokens);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
                },
                cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task AddAsync(TKey key, ReadOnlyMemory<char> text, CancellationToken cancellationToken = default)
        {
            await this.PerformWriteLockedActionAsync(
                async () =>
                {
                    await this.MutateAsync(
                        m =>
                        {
                            var tokens = ExtractDocumentTokens(text, this.DefaultTextExtractor, this.DefaultTokenizer, this.DefaultThesaurus);
                            this.AddForDefaultField(m, key, tokens);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
                },
                cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task AddAsync(TKey key, string text, CancellationToken cancellationToken = default)
        {
            await this.PerformWriteLockedActionAsync(
                async () =>
                {
                    await this.MutateAsync(
                        m =>
                        {
                            var tokens = ExtractDocumentTokens(text.AsMemory(), this.DefaultTextExtractor, this.DefaultTokenizer, this.DefaultThesaurus);
                            this.AddForDefaultField(m, key, tokens);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
                },
                cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task AddRangeAsync<TObject>(IEnumerable<TObject> items, CancellationToken cancellationToken = default)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var options = this.ObjectTypeConfiguration.Get<TObject>();
            await this.PerformWriteLockedActionAsync(
                async () =>
                {
                    await this.MutateAsync(
                        async m =>
                        {
                            foreach (var item in items)
                            {
                                await this.AddAsync(item, options, m, cancellationToken).ConfigureAwait(false);
                            }
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task AddAsync<TObject>(TObject item, CancellationToken cancellationToken = default)
        {
            var options = this.ObjectTypeConfiguration.Get<TObject>();
            await this.PerformWriteLockedActionAsync(
                async () => await this.MutateAsync(
                    async m => await this.AddAsync(item, options, m, cancellationToken).ConfigureAwait(false),
                    cancellationToken
                ).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> RemoveAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var documentRemoved = false;
            await this.PerformWriteLockedActionAsync(
                async () =>
                {
                    await this.MutateAsync(
                        m =>
                        {
                            documentRemoved = m.Metadata.Contains(key);
                            if (documentRemoved)
                            {
                                RemoveKeyFromIndex(key, m);
                            }
                        },
                        cancellationToken)
                        .ConfigureAwait(false);
                },
                cancellationToken)
                .ConfigureAwait(false);

            return documentRemoved;
        }

        /// <inheritdoc />
        public ISearchResults<TKey> Search(string searchText)
        {
            return this.Search(searchText, QueryExecutionOptions.None);
        }

        /// <inheritdoc />
        public ISearchResults<TKey> Search(IQuery query)
        {
            return this.Search(query, QueryExecutionOptions.None);
        }

        /// <inheritdoc />
        public ISearchResults<TKey> Search(string searchText, QueryExecutionOptions options = QueryExecutionOptions.None)
        {
            var query = this.QueryParser.Parse(this.FieldLookup, searchText, this);
            return this.Search(query, options);
        }

        /// <inheritdoc />
        public ISearchResults<TKey> Search(IQuery query, QueryExecutionOptions options = QueryExecutionOptions.None)
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (query is Query actualQuery && (options & QueryExecutionOptions.IncludeExecutionPlan) == QueryExecutionOptions.IncludeExecutionPlan)
            {
                return actualQuery.ExecuteWithTimings(this);
            }

            return new SearchResults<TKey>(this, query.Execute(this.currentSnapshot), null);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.Root.ToString();
        }

        private static List<Token> ExtractDocumentTokens(
            IEnumerable<ReadOnlyMemory<char>> documentTextFragments,
            ITextExtractor textExtractor,
            IIndexTokenizer tokenizer,
            IThesaurus thesaurus)
        {
            var documentOffset = 0;
            var fragments = Enumerable.Empty<DocumentTextFragment>();
            foreach (var documentText in documentTextFragments)
            {
                fragments = fragments.Concat(textExtractor.Extract(documentText, documentOffset));
                documentOffset += documentText.Length;
            }

            return TokenizeFragments(tokenizer, thesaurus, fragments);
        }

        private static List<Token> ExtractDocumentTokens(
            ReadOnlyMemory<char> documentText,
            ITextExtractor textExtractor,
            IIndexTokenizer tokenizer,
            IThesaurus thesaurus)
        {
            var fragments = textExtractor.Extract(documentText, 0);
            return TokenizeFragments(tokenizer, thesaurus, fragments);
        }

        private static List<Token> TokenizeFragments(IIndexTokenizer tokenizer, IThesaurus thesaurus, IEnumerable<DocumentTextFragment> fragments)
        {
            return tokenizer.Process(fragments).SelectMany(thesaurus.Process).ToList();
        }

        private void AddForDefaultField(IndexMutation<TKey> mutation, TKey key, List<Token> tokens)
        {
            var fieldId = this.FieldLookup.DefaultField;
            var documentId = this.GetUniqueIdForDocument(
                key,
                new DocumentStatistics(fieldId, tokens.CalculateTotalTokenCount()),
                mutation);

            IndexTokens(mutation, documentId, fieldId, tokens);
        }

        private void PerformWriteLockedAction(Action action)
        {
            if (!this.writeLock.Wait(this.writeLockTimeout))
            {
                throw new LiftiException(ExceptionMessages.TimeoutWaitingForWriteLock);
            }

            try
            {
                action();
            }
            finally
            {
                this.writeLock.Release();
            }
        }

        private async Task PerformWriteLockedActionAsync(Func<Task> asyncAction, CancellationToken cancellationToken)
        {
            if (!await this.writeLock.WaitAsync(this.writeLockTimeout, cancellationToken).ConfigureAwait(false))
            {
                throw new LiftiException(ExceptionMessages.TimeoutWaitingForWriteLock);
            }

            try
            {
                await asyncAction().ConfigureAwait(false);
            }
            finally
            {
                this.writeLock.Release();
            }
        }

        private async Task MutateAsync(Action<IndexMutation<TKey>> mutationAction, CancellationToken cancellationToken)
        {
            var indexMutation = this.GetCurrentMutationOrCreateTransient();

            mutationAction(indexMutation);

            if (indexMutation != this.batchMutation)
            {
                await this.ApplyMutationsAsync(indexMutation, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ApplyMutationsAsync(IndexMutation<TKey> indexMutation, CancellationToken cancellationToken)
        {
            this.metadata = indexMutation.Metadata;
            this.Root = indexMutation.Apply();
            if (this.indexModifiedActions != null)
            {
                foreach (var indexModifiedAction in this.indexModifiedActions)
                {
                    await indexModifiedAction(this.currentSnapshot, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private IndexMutation<TKey> GetCurrentMutationOrCreateTransient()
        {
            return this.batchMutation ?? new IndexMutation<TKey>(this.Root, this.metadata, this.IndexNodeFactory);
        }

        private async Task MutateAsync(Func<IndexMutation<TKey>, Task> asyncMutationAction, CancellationToken cancellationToken)
        {
            var indexMutation = this.GetCurrentMutationOrCreateTransient();

            await asyncMutationAction(indexMutation).ConfigureAwait(false);

            if (indexMutation != this.batchMutation)
            {
                await this.ApplyMutationsAsync(indexMutation, cancellationToken).ConfigureAwait(false);
            }
        }

        private static void IndexTokens(IndexMutation<TKey> indexMutation, int documentId, byte fieldId, IEnumerable<Token> tokens)
        {
            foreach (var token in tokens)
            {
                indexMutation.Add(documentId, fieldId, token);
            }
        }

        private static void RemoveKeyFromIndex(TKey key, IndexMutation<TKey> mutation)
        {
            var documentId = mutation.Metadata.Remove(key);

            mutation.Remove(documentId);
        }

        /// <remarks>This method is thread safe as we only allow one mutation operation at a time.</remarks>
        private async Task AddAsync<TObject>(TObject item, ObjectTypeConfiguration<TObject, TKey> options, IndexMutation<TKey> indexMutation, CancellationToken cancellationToken)
        {
            var key = options.KeyReader(item);

            var fieldTokens = new Dictionary<byte, List<Token>>();

            // First process any static field readers
            foreach (var field in options.FieldReaders.Values)
            {
                var (fieldId, textExtractor, tokenizer, thesaurus) = this.FieldLookup.GetFieldInfo(field.Name);

                var tokens = ExtractDocumentTokens(
                    await field.ReadAsync(item, cancellationToken).ConfigureAwait(false),
                    textExtractor,
                    tokenizer,
                    thesaurus);

                MergeFieldTokens(fieldTokens, key, field.Name, fieldId, tokens);
            }

            // Next process any dynamic field readers
            var objectType = typeof(TObject);
            foreach (var dynamicFieldReader in options.DynamicFieldReaders)
            {
                var dynamicFields = await dynamicFieldReader.ReadAsync(item, cancellationToken).ConfigureAwait(false);

                foreach (var (name, rawText) in dynamicFields)
                {
                    var (fieldId, textExtractor, tokenizer, thesaurus) = this.fieldLookup.GetOrCreateDynamicFieldInfo(dynamicFieldReader, name);

                    var tokens = ExtractDocumentTokens(rawText, textExtractor, tokenizer, thesaurus);

                    MergeFieldTokens(fieldTokens, key, name, fieldId, tokens);
                }
            }

            var documentStatistics = new DocumentStatistics(
                fieldTokens.ToDictionary(
                    t => t.Key,
                    t => t.Value.CalculateTotalTokenCount()));

            var documentId = this.GetUniqueIdForDocument(item, key, documentStatistics, options, indexMutation);

            foreach (var fieldTokenList in fieldTokens)
            {
                IndexTokens(indexMutation, documentId, fieldTokenList.Key, fieldTokenList.Value);
            }
        }

        private static void MergeFieldTokens(Dictionary<byte, List<Token>> fieldTokens, TKey key, string fieldName, byte fieldId, List<Token> tokens)
        {
            if (fieldTokens.ContainsKey(fieldId))
            {
                throw new LiftiException(ExceptionMessages.DuplicateFieldEncounteredOnObject, fieldName, key);
            }

            fieldTokens.Add(fieldId, tokens);
        }

        private int GetUniqueIdForDocument(TKey key, DocumentStatistics documentStatistics, IndexMutation<TKey> mutation)
        {
            this.EnforceDuplicateKeyBehavior(key, mutation);

            return mutation.Metadata.Add(key, documentStatistics);
        }

        private int GetUniqueIdForDocument<TObject>(TObject item, TKey key, DocumentStatistics documentStatistics, ObjectTypeConfiguration<TObject, TKey> options, IndexMutation<TKey> mutation)
        {
            this.EnforceDuplicateKeyBehavior(key, mutation);

            return mutation.Metadata.Add(key, item, documentStatistics, options);
        }

        private void EnforceDuplicateKeyBehavior(TKey key, IndexMutation<TKey> mutation)
        {
            if (this.indexOptions.DuplicateKeyBehavior == DuplicateKeyBehavior.Replace)
            {
                if (mutation.Metadata.Contains(key))
                {
                    RemoveKeyFromIndex(key, mutation);
                }
            }
        }

        /// <summary>
        /// Dispose this instance.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> if this instance is being called from the <see cref="Dispose()"/> method.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.writeLock.Dispose();
                }

                this.isDisposed = true;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Rehydrates any dynamic fields to the index, and returns mapping between the fields ids as serialized in the index, 
        /// and the ids in the new index. Assuming the index has been rebuilt with exactly the same configuration, the ids
        /// will match on each side of the map.
        /// </summary>
        internal SerializedFieldIdMap MapSerializedFieldIds(List<SerializedFieldInfo> serializedFields)
        {
            var fieldMap = new Dictionary<byte, byte>
            {
                // Always map the default field to itself
                {  this.fieldLookup.DefaultField, this.fieldLookup.DefaultField }
            };

            foreach (var field in serializedFields)
            {
                byte newFieldId;
                switch (field.Kind)
                {
                    case FieldKind.Dynamic:
                        if (field.DynamicFieldReaderName == null)
                        {
                            throw new LiftiException(ExceptionMessages.NoDynamicFieldReaderNameInDynamicField);
                        }

                        var newDynamicField = this.fieldLookup.GetOrCreateDynamicFieldInfo(field.DynamicFieldReaderName, field.Name);
                        newFieldId = newDynamicField.Id;
                        break;
                    case FieldKind.Static:
                        var fieldInfo = this.fieldLookup.GetFieldInfo(field.Name);
                        newFieldId = fieldInfo.Id;
                        break;
                    default:
                        throw new LiftiException(ExceptionMessages.UnknownFieldKind, field.Kind);

                }

                fieldMap[field.FieldId] = newFieldId;
            }

            return new SerializedFieldIdMap(fieldMap);
        }
    }
}
