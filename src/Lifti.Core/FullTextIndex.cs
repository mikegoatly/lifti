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
        private readonly IQueryParser queryParser;
        private readonly Func<IIndexSnapshot<TKey>, CancellationToken, Task>[]? indexModifiedActions;
        private readonly IndexOptions indexOptions;
        private readonly IdPool<TKey> idPool;
        private readonly IIndexNavigatorPool indexNavigatorPool;
        private readonly SemaphoreSlim writeLock = new SemaphoreSlim(1);
        private readonly TimeSpan writeLockTimeout = TimeSpan.FromSeconds(10);
        private bool isDisposed;

        /// <remarks>
        /// currentSnapshot and root are set via the Root property when it is initialized by the constructor
        /// </remarks>
        private IndexSnapshot<TKey> currentSnapshot = null!;
        private IndexNode root = null!;

        private IndexMutation? batchMutation;

        internal FullTextIndex(
            IndexOptions indexOptions,
            ConfiguredObjectTokenizationOptions<TKey> itemTokenizationOptions,
            IIndexNodeFactory indexNodeFactory,
            IQueryParser queryParser,
            IIndexScorerFactory scorer,
            ITextExtractor defaultTextExtractor,
            IIndexTokenizer defaultTokenizer,
            Func<IIndexSnapshot<TKey>, CancellationToken, Task>[]? indexModifiedActions)
        {
            this.indexNavigatorPool = new IndexNavigatorPool(scorer);
            this.indexOptions = indexOptions;
            this.ItemTokenizationOptions = itemTokenizationOptions ?? throw new ArgumentNullException(nameof(itemTokenizationOptions));
            this.IndexNodeFactory = indexNodeFactory ?? throw new ArgumentNullException(nameof(indexNodeFactory));
            this.queryParser = queryParser ?? throw new ArgumentNullException(nameof(queryParser));
            this.DefaultTextExtractor = defaultTextExtractor;
            this.DefaultTokenizer = defaultTokenizer ?? throw new ArgumentNullException(nameof(defaultTokenizer));
            this.indexModifiedActions = indexModifiedActions;
            this.idPool = new IdPool<TKey>();
            this.FieldLookup = new IndexedFieldLookup(
                itemTokenizationOptions.GetAllConfiguredFields(),
                defaultTextExtractor,
                defaultTokenizer);

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
        public IItemStore<TKey> Items => this.idPool;

        internal IIdPool<TKey> IdPool => this.idPool;

        /// <inheritdoc />
        public IIndexedFieldLookup FieldLookup { get; }

        /// <inheritdoc />
        public int Count => this.currentSnapshot.Items.Count;

        internal IIndexNodeFactory IndexNodeFactory { get; }

        /// <inheritdoc />
        public IQueryParser QueryParser => this.queryParser;

        /// <inheritdoc />
        public IIndexSnapshot<TKey> Snapshot => this.currentSnapshot;

        /// <inheritdoc />
        public IIndexTokenizer DefaultTokenizer { get; }

        /// <inheritdoc />
        public ITextExtractor DefaultTextExtractor { get; }

        internal ConfiguredObjectTokenizationOptions<TKey> ItemTokenizationOptions { get; }

        /// <inheritdoc />
        IIndexTokenizer IIndexTokenizerProvider.this[string fieldName] => this.FieldLookup.GetFieldInfo(fieldName).Tokenizer;

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
                            var tokens = ExtractDocumentTokens(text, this.DefaultTextExtractor, this.DefaultTokenizer);
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
                            var tokens = ExtractDocumentTokens(text, this.DefaultTextExtractor, this.DefaultTokenizer);
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

            var options = this.ItemTokenizationOptions.Get<TItem>();
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
            var options = this.ItemTokenizationOptions.Get<TItem>();
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
                    if (!this.idPool.Contains(itemKey))
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
            var query = this.queryParser.Parse(this.FieldLookup, searchText, this);
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

        private static IReadOnlyCollection<Token> ExtractDocumentTokens(IEnumerable<string> documentTextFragments, ITextExtractor textExtractor, IIndexTokenizer tokenizer)
        {
            var documentOffset = 0;
            var fragments = Enumerable.Empty<DocumentTextFragment>();
            foreach (var documentText in documentTextFragments)
            {
                fragments = fragments.Concat(textExtractor.Extract(documentText.AsMemory(), documentOffset));
                documentOffset += documentText.Length;
            }

            return tokenizer.Process(fragments);
        }

        private static IReadOnlyCollection<Token> ExtractDocumentTokens(string documentText, ITextExtractor textExtractor, IIndexTokenizer tokenizer)
        {
            var fragments = textExtractor.Extract(documentText.AsMemory(), 0);
            return tokenizer.Process(fragments);
        }

        private void AddForDefaultField(IndexMutation mutation, TKey itemKey, IReadOnlyCollection<Token> tokens)
        {
            var fieldId = this.FieldLookup.DefaultField;
            var itemId = this.GetUniqueIdForItem(
                itemKey,
                new DocumentStatistics(fieldId, CalculateTotalTokenCount(tokens)),
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

        private static int CalculateTotalTokenCount(IReadOnlyCollection<Token> tokens)
        {
            return tokens.Aggregate(0, (current, token) => current + token.Locations.Count);
        }

        private void RemoveKeyFromIndex(TKey itemKey, IndexMutation mutation)
        {
            var id = this.idPool.ReleaseItem(itemKey);
            mutation.Remove(id);
        }

        private async Task AddAsync<TItem>(TItem item, ObjectTokenization<TItem, TKey> options, IndexMutation indexMutation, CancellationToken cancellationToken)
        {
            var itemKey = options.KeyReader(item);

            var fieldTokens = new List<(byte fieldId, IReadOnlyCollection<Token> tokens)>(options.FieldReaders.Count);
            foreach (var field in options.FieldReaders.Values)
            {
                var (fieldId, textExtractor, tokenizer) = this.FieldLookup.GetFieldInfo(field.Name);
                var tokens = ExtractDocumentTokens(await field.ReadAsync(item, cancellationToken).ConfigureAwait(false), textExtractor, tokenizer);
                fieldTokens.Add((fieldId, tokens));
            }

            var documentStatistics = new DocumentStatistics(
                fieldTokens.ToDictionary(
                    t => t.fieldId,
                    t => CalculateTotalTokenCount(t.tokens)));

            var itemId = this.GetUniqueIdForItem(itemKey, documentStatistics, indexMutation);

            foreach (var (fieldId, tokens) in fieldTokens)
            {
                IndexTokens(indexMutation, itemId, fieldId, tokens);
            }
        }

        private int GetUniqueIdForItem(TKey itemKey, DocumentStatistics documentStatistics, IndexMutation mutation)
        {
            if (this.indexOptions.DuplicateItemBehavior == DuplicateItemBehavior.ReplaceItem)
            {
                if (this.idPool.Contains(itemKey))
                {
                    this.RemoveKeyFromIndex(itemKey, mutation);
                }
            }

            return this.idPool.Add(itemKey, documentStatistics);
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
    }
}
