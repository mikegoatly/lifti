using Lifti.ItemTokenization;
using Lifti.Querying;
using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti
{
    public class FullTextIndex<TKey> : IFullTextIndex<TKey>, IDisposable
    {
        private readonly ITokenizerFactory tokenizerFactory;
        private readonly IQueryParser queryParser;
        private readonly TokenizationOptions defaultTokenizationOptions;
        private readonly Func<IIndexSnapshot<TKey>, ValueTask>[] indexModifiedActions;
        private readonly ConfiguredItemTokenizationOptions<TKey> itemTokenizationOptions;
        private readonly IdPool<TKey> idPool;
        private readonly IIndexNavigatorPool indexNavigatorPool = new IndexNavigatorPool();
        private readonly SemaphoreSlim writeLock = new SemaphoreSlim(1);
        private readonly TimeSpan writeLockTimeout = TimeSpan.FromSeconds(10);
        private IndexSnapshot<TKey> currentSnapshot;
        private bool isDisposed;
        private IndexNode root;

        private IndexMutation batchMutation;

        internal FullTextIndex(
            ConfiguredItemTokenizationOptions<TKey> itemTokenizationOptions,
            IIndexNodeFactory indexNodeFactory,
            ITokenizerFactory tokenizerFactory,
            IQueryParser queryParser,
            TokenizationOptions defaultTokenizationOptions,
            Func<IIndexSnapshot<TKey>, ValueTask>[] indexModifiedActions)
        {
            this.itemTokenizationOptions = itemTokenizationOptions ?? throw new ArgumentNullException(nameof(itemTokenizationOptions));
            this.IndexNodeFactory = indexNodeFactory ?? throw new ArgumentNullException(nameof(indexNodeFactory));
            this.tokenizerFactory = tokenizerFactory ?? throw new ArgumentNullException(nameof(tokenizerFactory));
            this.queryParser = queryParser ?? throw new ArgumentNullException(nameof(queryParser));
            this.defaultTokenizationOptions = defaultTokenizationOptions ?? throw new ArgumentNullException(nameof(defaultTokenizationOptions));
            this.indexModifiedActions = indexModifiedActions;
            this.idPool = new IdPool<TKey>();
            this.FieldLookup = new IndexedFieldLookup(
                this.itemTokenizationOptions.GetAllConfiguredFields(),
                tokenizerFactory,
                defaultTokenizationOptions);

            this.Root = this.IndexNodeFactory.CreateRootNode();
        }

        public IndexNode Root
        {
            get => this.root;
            private set
            {
                this.root = value;
                this.currentSnapshot = new IndexSnapshot<TKey>(this.indexNavigatorPool, this);
            }
        }

        public IIdLookup<TKey> IdLookup => this.idPool;

        internal IIdPool<TKey> IdPool => this.idPool;

        public IIndexedFieldLookup FieldLookup { get; }

        public int Count => this.currentSnapshot.IdLookup.Count;

        internal IIndexNodeFactory IndexNodeFactory { get; }

        public IIndexSnapshot<TKey> Snapshot => this.currentSnapshot;

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

        public async ValueTask CommitBatchChangeAsync()
        {
            await this.PerformWriteLockedActionAsync(async () =>
            {
                if (this.batchMutation == null)
                {
                    throw new LiftiException(ExceptionMessages.NoBatchChangeInProgress);
                }

                await this.ApplyMutationsAsync(this.batchMutation).ConfigureAwait(false);

                this.batchMutation = null;
            });
        }

        public async ValueTask AddAsync(TKey itemKey, IEnumerable<string> text, TokenizationOptions tokenizationOptions = null)
        {
            await this.PerformWriteLockedActionAsync(async () =>
                {
                    var itemId = this.idPool.Add(itemKey);

                    var tokenizer = this.GetTokenizer(tokenizationOptions);
                    await this.MutateAsync(m =>
                    {
                        foreach (var word in tokenizer.Process(text))
                        {
                            m.Add(itemId, this.FieldLookup.DefaultField, word);
                        }
                    });
                });
        }

        public async ValueTask AddAsync(TKey itemKey, string text, TokenizationOptions tokenizationOptions = null)
        {
            await this.PerformWriteLockedActionAsync(async () =>
                {
                    var itemId = this.idPool.Add(itemKey);

                    var tokenizer = this.GetTokenizer(tokenizationOptions);
                    await this.MutateAsync(m =>
                    {
                        foreach (var word in tokenizer.Process(text))
                        {
                            m.Add(itemId, this.FieldLookup.DefaultField, word);
                        }
                    });
                });
        }

        public async ValueTask AddRangeAsync<TItem>(IEnumerable<TItem> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var options = this.itemTokenizationOptions.Get<TItem>();
            await this.PerformWriteLockedActionAsync(async () =>
                {
                    await this.MutateAsync(m =>
                    {
                        foreach (var item in items)
                        {
                            this.Add(item, options, m);
                        }
                    });
                });
        }

        public async ValueTask AddAsync<TItem>(TItem item)
        {
            var options = this.itemTokenizationOptions.Get<TItem>();
            await this.PerformWriteLockedActionAsync(
                async () => await this.MutateAsync(async m => await this.AddAsync(item, options, m)));
        }

        public async ValueTask<bool> RemoveAsync(TKey itemKey)
        {
            var result = false;
            await this.PerformWriteLockedActionAsync(async () =>
            {
                if (!this.idPool.Contains(itemKey))
                {
                    result = false;
                    return;
                }

                await this.MutateAsync(m =>
                {
                    var id = this.idPool.ReleaseItem(itemKey);
                    m.Remove(id);
                });

                result = true;
            });

            return result;
        }

        public IEnumerable<SearchResult<TKey>> Search(string searchText, TokenizationOptions tokenizationOptions = null)
        {
            var query = this.queryParser.Parse(this.FieldLookup, searchText, this.GetTokenizer(tokenizationOptions));
            return query.Execute(this.currentSnapshot);
        }

        public override string ToString()
        {
            return this.Root.ToString();
        }

        internal void SetRootWithLock(IndexNode indexNode)
        {
            this.PerformWriteLockedAction(() => this.Root = indexNode);
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

        private async ValueTask PerformWriteLockedActionAsync(Func<ValueTask> asyncAction)
        {
            if (!await this.writeLock.WaitAsync(this.writeLockTimeout).ConfigureAwait(false))
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

        private async ValueTask MutateAsync(Action<IndexMutation> mutationAction)
        {
            var indexMutation = this.GetCurrentMutationOrCreateTransient();

            mutationAction(indexMutation);

            if (indexMutation != this.batchMutation)
            {
                await this.ApplyMutationsAsync(indexMutation).ConfigureAwait(false);
            }
        }

        private async Task ApplyMutationsAsync(IndexMutation indexMutation)
        {
            this.Root = indexMutation.Apply();
            if (this.indexModifiedActions != null)
            {
                foreach (var indexModifiedAction in this.indexModifiedActions)
                {
                    await indexModifiedAction(this.currentSnapshot);
                }
            }
        }

        private IndexMutation GetCurrentMutationOrCreateTransient()
        {
            return this.batchMutation ?? new IndexMutation(this.Root, this.IndexNodeFactory);
        }

        private async ValueTask MutateAsync(Func<IndexMutation, Task> asyncMutationAction)
        {
            var indexMutation = this.GetCurrentMutationOrCreateTransient();

            await asyncMutationAction(indexMutation).ConfigureAwait(false);

            if (indexMutation != this.batchMutation)
            {
                await this.ApplyMutationsAsync(indexMutation).ConfigureAwait(false);
            }
        }

        private ITokenizer GetTokenizer(TokenizationOptions tokenizationOptions)
        {
            return this.tokenizerFactory.Create(tokenizationOptions ?? this.defaultTokenizationOptions);
        }

        private void Add<TItem>(TItem item, ItemTokenizationOptions<TItem, TKey> options, IndexMutation indexMutation)
        {
            var itemKey = options.KeyReader(item);
            var itemId = this.idPool.Add(itemKey);

            foreach (var field in options.FieldTokenization)
            {
                var (fieldId, tokenizer) = this.FieldLookup.GetFieldInfo(field.Name);
                var tokens = field.Tokenize(tokenizer, item);
                IndexTokens(indexMutation, itemId, fieldId, tokens);
            }
        }

        private static void IndexTokens(IndexMutation indexMutation, int itemId, byte fieldId, IEnumerable<Token> tokens)
        {
            foreach (var word in tokens)
            {
                indexMutation.Add(itemId, fieldId, word);
            }
        }

        private async ValueTask AddAsync<TItem>(TItem item, ItemTokenizationOptions<TItem, TKey> options, IndexMutation indexMutation)
        {
            var itemKey = options.KeyReader(item);
            var itemId = this.idPool.Add(itemKey);

            foreach (var field in options.FieldTokenization)
            {
                var (fieldId, tokenizer) = this.FieldLookup.GetFieldInfo(field.Name);
                var tokens = await field.TokenizeAsync(tokenizer, item);

                IndexTokens(indexMutation, itemId, fieldId, tokens);
            }
        }


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

        public void Dispose()
        {
            this.Dispose(true);
        }
    }
}
