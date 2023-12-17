using Lifti.Querying;
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
        private readonly ItemStore<TKey> itemStore;
        private readonly IIndexNavigatorPool indexNavigatorPool;
        private readonly SemaphoreSlim writeLock = new(1);
        private readonly TimeSpan writeLockTimeout = TimeSpan.FromSeconds(10);
        private readonly IndexedFieldLookup fieldLookup;
        private bool isDisposed;

        /// <remarks>
        /// currentSnapshot and root are set via the Root property when it is initialized by the constructor
        /// </remarks>
        private IndexSnapshot<TKey> currentSnapshot = null!;
        private IndexNode root = null!;

        private IndexMutation? batchMutation;

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
            this.itemStore = new ItemStore<TKey>();

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
                this.currentSnapshot = new IndexSnapshot<TKey>(this.indexNavigatorPool, this);
            }
        }

        /// <inheritdoc />
        public IItemStore<TKey> Items => this.itemStore;

        /// <inheritdoc />
        public IIndexedFieldLookup FieldLookup => this.fieldLookup;

        /// <inheritdoc />
        public int Count => this.currentSnapshot.Items.Count;

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

                this.batchMutation = new IndexMutation(this.Root, this.IndexNodeFactory);
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
        public async Task AddAsync(TKey itemKey, IEnumerable<string> text, CancellationToken cancellationToken = default)
        {
            await this.PerformWriteLockedActionAsync(
                async () =>
                {
                    await this.MutateAsync(
                        m =>
                        {
                            var tokens = ExtractDocumentTokens(text, this.DefaultTextExtractor, this.DefaultTokenizer, this.DefaultThesaurus);
                            this.AddForDefaultField(m, itemKey, tokens);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
                },
                cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task AddAsync(TKey itemKey, string text, CancellationToken cancellationToken = default)
        {
            await this.PerformWriteLockedActionAsync(
                async () =>
                {
                    await this.MutateAsync(
                        m =>
                        {
                            var tokens = ExtractDocumentTokens(text, this.DefaultTextExtractor, this.DefaultTokenizer, this.DefaultThesaurus);
                            this.AddForDefaultField(m, itemKey, tokens);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
                },
                cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task AddRangeAsync<TItem>(IEnumerable<TItem> items, CancellationToken cancellationToken = default)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var options = this.ObjectTypeConfiguration.Get<TItem>();
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
        public async Task AddAsync<TItem>(TItem item, CancellationToken cancellationToken = default)
        {
            var options = this.ObjectTypeConfiguration.Get<TItem>();
            await this.PerformWriteLockedActionAsync(
                async () => await this.MutateAsync(
                    async m => await this.AddAsync(item, options, m, cancellationToken).ConfigureAwait(false),
                    cancellationToken
                ).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> RemoveAsync(TKey itemKey, CancellationToken cancellationToken = default)
        {
            var result = false;
            await this.PerformWriteLockedActionAsync(
                async () =>
                {
                    if (!this.Items.Contains(itemKey))
                    {
                        result = false;
                        return;
                    }

                    await this.MutateAsync(
                        m => this.RemoveKeyFromIndex(itemKey, m),
                        cancellationToken)
                        .ConfigureAwait(false);

                    result = true;
                },
                cancellationToken)
                .ConfigureAwait(false);

            return result;
        }

        /// <inheritdoc />
        public ISearchResults<TKey> Search(string searchText)
        {
            var query = this.QueryParser.Parse(this.FieldLookup, searchText, this);
            return this.Search(query);
        }

        /// <inheritdoc />
        public ISearchResults<TKey> Search(IQuery query)
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return new SearchResults<TKey>(this, query.Execute(this.currentSnapshot));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.Root.ToString();
        }

        internal void SetRootWithLock(IndexNode indexNode)
        {
            this.PerformWriteLockedAction(() => this.Root = indexNode);
        }

        private static List<Token> ExtractDocumentTokens(
            IEnumerable<string> documentTextFragments,
            ITextExtractor textExtractor,
            IIndexTokenizer tokenizer,
            IThesaurus thesaurus)
        {
            var documentOffset = 0;
            var fragments = Enumerable.Empty<DocumentTextFragment>();
            foreach (var documentText in documentTextFragments)
            {
                fragments = fragments.Concat(textExtractor.Extract(documentText.AsMemory(), documentOffset));
                documentOffset += documentText.Length;
            }

            return TokenizeFragments(tokenizer, thesaurus, fragments);
        }

        private static List<Token> ExtractDocumentTokens(
            string documentText,
            ITextExtractor textExtractor,
            IIndexTokenizer tokenizer,
            IThesaurus thesaurus)
        {
            var fragments = textExtractor.Extract(documentText.AsMemory(), 0);
            return TokenizeFragments(tokenizer, thesaurus, fragments);
        }

        private static List<Token> TokenizeFragments(IIndexTokenizer tokenizer, IThesaurus thesaurus, IEnumerable<DocumentTextFragment> fragments)
        {
            return tokenizer.Process(fragments).SelectMany(thesaurus.Process).ToList();
        }

        private void AddForDefaultField(IndexMutation mutation, TKey itemKey, List<Token> tokens)
        {
            var fieldId = this.FieldLookup.DefaultField;
            var itemId = this.GetUniqueIdForItem(
                itemKey,
                new DocumentStatistics(fieldId, tokens.CalculateTotalTokenCount()),
                mutation);

            IndexTokens(mutation, itemId, fieldId, tokens);
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

        private async Task MutateAsync(Action<IndexMutation> mutationAction, CancellationToken cancellationToken)
        {
            var indexMutation = this.GetCurrentMutationOrCreateTransient();

            mutationAction(indexMutation);

            if (indexMutation != this.batchMutation)
            {
                await this.ApplyMutationsAsync(indexMutation, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ApplyMutationsAsync(IndexMutation indexMutation, CancellationToken cancellationToken)
        {
            this.Root = indexMutation.Apply();
            if (this.indexModifiedActions != null)
            {
                foreach (var indexModifiedAction in this.indexModifiedActions)
                {
                    await indexModifiedAction(this.currentSnapshot, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private IndexMutation GetCurrentMutationOrCreateTransient()
        {
            return this.batchMutation ?? new IndexMutation(this.Root, this.IndexNodeFactory);
        }

        private async Task MutateAsync(Func<IndexMutation, Task> asyncMutationAction, CancellationToken cancellationToken)
        {
            var indexMutation = this.GetCurrentMutationOrCreateTransient();

            await asyncMutationAction(indexMutation).ConfigureAwait(false);

            if (indexMutation != this.batchMutation)
            {
                await this.ApplyMutationsAsync(indexMutation, cancellationToken).ConfigureAwait(false);
            }
        }

        private static void IndexTokens(IndexMutation indexMutation, int itemId, byte fieldId, IEnumerable<Token> tokens)
        {
            foreach (var token in tokens)
            {
                indexMutation.Add(itemId, fieldId, token);
            }
        }

        private void RemoveKeyFromIndex(TKey itemKey, IndexMutation mutation)
        {
            var id = this.itemStore.Remove(itemKey);

            mutation.Remove(id);
        }

        /// <remarks>This method is thread safe as we only allow one mutation operation at a time.</remarks>
        private async Task AddAsync<TItem>(TItem item, IndexedObjectConfiguration<TItem, TKey> options, IndexMutation indexMutation, CancellationToken cancellationToken)
        {
            var itemKey = options.KeyReader(item);

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

                MergeFieldTokens(fieldTokens, itemKey, field.Name, fieldId, tokens);
            }

            // Next process any dynamic field readers
            var itemType = typeof(TItem);
            foreach (var dynamicFieldReader in options.DynamicFieldReaders)
            {
                var dynamicFields = await dynamicFieldReader.ReadAsync(item, cancellationToken).ConfigureAwait(false);

                foreach (var (name, rawText) in dynamicFields)
                {
                    var (fieldId, textExtractor, tokenizer, thesaurus) = this.fieldLookup.GetOrCreateDynamicFieldInfo(dynamicFieldReader, name);

                    var tokens = ExtractDocumentTokens(rawText, textExtractor, tokenizer, thesaurus);

                    MergeFieldTokens(fieldTokens, itemKey, name, fieldId, tokens);
                }
            }

            var documentStatistics = new DocumentStatistics(
                fieldTokens.ToDictionary(
                    t => t.Key,
                    t => t.Value.CalculateTotalTokenCount()));

            var itemId = this.GetUniqueIdForItem(item, itemKey, documentStatistics, options, indexMutation);

            foreach (var fieldTokenList in fieldTokens)
            {
                IndexTokens(indexMutation, itemId, fieldTokenList.Key, fieldTokenList.Value);
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

        private int GetUniqueIdForItem(TKey itemKey, DocumentStatistics documentStatistics, IndexMutation mutation)
        {
            if (this.indexOptions.DuplicateItemBehavior == DuplicateItemBehavior.ReplaceItem)
            {
                if (this.itemStore.Contains(itemKey))
                {
                    this.RemoveKeyFromIndex(itemKey, mutation);
                }
            }

            return this.itemStore.Add(itemKey, documentStatistics);
        }

        private int GetUniqueIdForItem<TItem>(TItem item, TKey itemKey, DocumentStatistics documentStatistics, IndexedObjectConfiguration<TItem, TKey> options, IndexMutation mutation)
        {
            if (this.indexOptions.DuplicateItemBehavior == DuplicateItemBehavior.ReplaceItem)
            {
                if (this.itemStore.Contains(itemKey))
                {
                    this.RemoveKeyFromIndex(itemKey, mutation);
                }
            }

            return this.itemStore.Add(itemKey, item, documentStatistics, options);
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
        internal Dictionary<byte, byte> RehydrateSerializedFields(List<SerializedFieldInfo> serializedFields)
        {
            var fieldMap = new Dictionary<byte, byte>
            {
                // Always map the default field to itself
                {  this.fieldLookup.DefaultField, this.fieldLookup.DefaultField }
            };

            foreach (var field in serializedFields)
            {
                byte newId;
                switch (field.Kind)
                {
                    case FieldKind.Dynamic:
                        if (field.DynamicFieldReaderName == null)
                        {
                            throw new LiftiException(ExceptionMessages.NoDynamicFieldReaderNameInDynamicField);
                        }

                        var newDynamicField = this.fieldLookup.GetOrCreateDynamicFieldInfo(field.DynamicFieldReaderName, field.Name);
                        newId = newDynamicField.Id;
                        break;
                    case FieldKind.Static:
                        var fieldInfo = this.fieldLookup.GetFieldInfo(field.Name);
                        newId = fieldInfo.Id;
                        break;
                    default:
                        throw new LiftiException(ExceptionMessages.UnknownFieldKind, field.Kind);

                }

                fieldMap[field.FieldId] = newId;
            }

            return fieldMap;
        }
    }
}
