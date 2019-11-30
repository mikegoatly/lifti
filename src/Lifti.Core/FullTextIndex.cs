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
        private readonly ConfiguredItemTokenizationOptions<TKey> itemTokenizationOptions;
        private readonly IdPool<TKey> idPool;
        private readonly IIndexNavigatorPool indexNavigatorPool = new IndexNavigatorPool();
        private readonly SemaphoreSlim writeLock = new SemaphoreSlim(1);
        private readonly TimeSpan writeLockTimeout = TimeSpan.FromSeconds(10);
        private IndexSnapshot<TKey> currentSnapshot;
        private bool isDisposed;
        private IndexNode root;

        internal FullTextIndex(
            ConfiguredItemTokenizationOptions<TKey> itemTokenizationOptions,
            IIndexNodeFactory indexNodeFactory,
            ITokenizerFactory tokenizerFactory,
            IQueryParser queryParser,
            TokenizationOptions defaultTokenizationOptions)
        {
            this.itemTokenizationOptions = itemTokenizationOptions ?? throw new ArgumentNullException(nameof(itemTokenizationOptions));
            this.IndexNodeFactory = indexNodeFactory ?? throw new ArgumentNullException(nameof(indexNodeFactory));
            this.tokenizerFactory = tokenizerFactory ?? throw new ArgumentNullException(nameof(tokenizerFactory));
            this.queryParser = queryParser ?? throw new ArgumentNullException(nameof(queryParser));
            this.defaultTokenizationOptions = defaultTokenizationOptions ?? throw new ArgumentNullException(nameof(defaultTokenizationOptions));

            this.idPool = new IdPool<TKey>();
            this.FieldLookup = new IndexedFieldLookup(
                this.itemTokenizationOptions.GetAllConfiguredFields(),
                tokenizerFactory,
                defaultTokenizationOptions);

            this.Root = this.IndexNodeFactory.CreateRootNode();
        }

        public IndexNode Root
        {
            get => root;
            private set
            {
                root = value;
                this.currentSnapshot = new IndexSnapshot<TKey>(this.indexNavigatorPool, this);
            }
        }

        public IIdLookup<TKey> IdLookup => this.idPool;

        internal IIdPool<TKey> IdPool => this.idPool;

        public IIndexedFieldLookup FieldLookup { get; }

        public int Count => this.IdLookup.Count;

        internal IIndexNodeFactory IndexNodeFactory { get; }

        public IIndexSnapshot<TKey> Snapshot()
        {
            return this.currentSnapshot;
        }

        public void Add(TKey itemKey, IEnumerable<string> text, TokenizationOptions tokenizationOptions = null)
        {
            this.PerformWriteLockedAction(() =>
                {
                    var itemId = this.idPool.Add(itemKey);

                    var tokenizer = this.GetTokenizer(tokenizationOptions);
                    this.ApplyMutations(m =>
                    {
                        foreach (var word in tokenizer.Process(text))
                        {
                            m.Add(itemId, this.FieldLookup.DefaultField, word);
                        }
                    });
                });
        }

        public void Add(TKey itemKey, string text, TokenizationOptions tokenizationOptions = null)
        {
            this.PerformWriteLockedAction(() =>
                {
                    var itemId = this.idPool.Add(itemKey);

                    var tokenizer = this.GetTokenizer(tokenizationOptions);
                    this.ApplyMutations(m =>
                    {
                        foreach (var word in tokenizer.Process(text))
                        {
                            m.Add(itemId, this.FieldLookup.DefaultField, word);
                        }
                    });
                });
        }

        public void AddRange<TItem>(IEnumerable<TItem> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var options = this.itemTokenizationOptions.Get<TItem>();
            this.PerformWriteLockedAction(() =>
                {
                    this.ApplyMutations(m =>
                    {
                        foreach (var item in items)
                        {
                            this.Add(item, options, m);
                        }
                    });
                });
        }

        public void Add<TItem>(TItem item)
        {
            var options = this.itemTokenizationOptions.Get<TItem>();
            this.PerformWriteLockedAction(() => this.ApplyMutations(m => this.Add(item, options, m)));
        }

        public async ValueTask AddRangeAsync<TItem>(IEnumerable<TItem> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var options = this.itemTokenizationOptions.Get<TItem>();

            await this.PerformWriteLockedActionAsync(() =>
            {
                return this.ApplyIndexInsertionMutationsAsync(async m =>
                {
                    foreach (var item in items)
                    {
                        await this.AddAsync(item, options, m);
                    }
                });
            }).ConfigureAwait(false);
        }

        public async ValueTask AddAsync<TItem>(TItem item)
        {
            var options = this.itemTokenizationOptions.Get<TItem>();

            await this.PerformWriteLockedActionAsync(
                () => this.ApplyIndexInsertionMutationsAsync(async m => await this.AddAsync(item, options, m))
            ).ConfigureAwait(false);
        }

        public bool Remove(TKey itemKey)
        {
            var result = false;
            this.PerformWriteLockedAction(() =>
            {
                if (!this.idPool.Contains(itemKey))
                {
                    result = false;
                    return;
                }

                this.ApplyMutations(m =>
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

        private async Task PerformWriteLockedActionAsync(Func<Task> asyncAction)
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

        private void ApplyMutations(Action<IndexMutation> mutationAction)
        {
            var indexMutation = new IndexMutation(this.Root, this.IndexNodeFactory);

            mutationAction(indexMutation);

            this.Root = indexMutation.ApplyInsertions();
        }

        private async Task ApplyIndexInsertionMutationsAsync(Func<IndexMutation, Task> asyncMutationAction)
        {
            var indexMutation = new IndexMutation(this.Root, this.IndexNodeFactory);

            await asyncMutationAction(indexMutation).ConfigureAwait(false);

            this.Root = indexMutation.ApplyInsertions();
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
            Dispose(true);
        }
    }
}
