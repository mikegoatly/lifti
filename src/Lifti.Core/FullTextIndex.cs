using Lifti.ItemTokenization;
using Lifti.Querying;
using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lifti
{
    public class FullTextIndex<TKey> : IFullTextIndex<TKey>
    {
        private readonly ITokenizerFactory tokenizerFactory;
        private readonly IQueryParser queryParser;
        private readonly TokenizationOptions defaultTokenizationOptions;
        private readonly ConfiguredItemTokenizationOptions<TKey> itemTokenizationOptions;
        private readonly IdPool<TKey> idPool;
        private readonly IIndexNavigatorPool indexNavigatorPool = new IndexNavigatorPool();

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

        internal IndexNode Root { get; set; }

        public IIdLookup<TKey> IdLookup => this.idPool;

        internal IIdPool<TKey> IdPool => this.idPool;

        public IIndexedFieldLookup FieldLookup { get; }

        public int Count => this.IdLookup.Count;

        internal IIndexNodeFactory IndexNodeFactory { get; }

        public IIndexNavigator CreateNavigator()
        {
            return this.indexNavigatorPool.Create(this.Root);
        }

        public void Add(TKey itemKey, IEnumerable<string> text, TokenizationOptions tokenizationOptions = null)
        {
            // TODO lock for writing on all mutations
            var itemId = this.idPool.Add(itemKey);

            var tokenizer = this.GetTokenizer(tokenizationOptions);
            this.ApplyIndexInsertionMutations(m =>
            {
                foreach (var word in tokenizer.Process(text))
                {
                    m.Add(itemId, this.FieldLookup.DefaultField, word);
                }
            });
        }

        public void Add(TKey itemKey, string text, TokenizationOptions tokenizationOptions = null)
        {
            var itemId = this.idPool.Add(itemKey);

            var tokenizer = this.GetTokenizer(tokenizationOptions);
            this.ApplyIndexInsertionMutations(m =>
            {
                foreach (var word in tokenizer.Process(text))
                {
                    m.Add(itemId, this.FieldLookup.DefaultField, word);
                }
            });
        }

        public void AddRange<TItem>(IEnumerable<TItem> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var options = this.itemTokenizationOptions.Get<TItem>();

            this.ApplyIndexInsertionMutations(m =>
            {
                foreach (var item in items)
                {
                    this.Add(item, options, m);
                }
            });
        }

        public void Add<TItem>(TItem item)
        {
            var options = this.itemTokenizationOptions.Get<TItem>();
            this.ApplyIndexInsertionMutations(m => this.Add(item, options, m));
        }

        public async ValueTask AddRangeAsync<TItem>(IEnumerable<TItem> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var options = this.itemTokenizationOptions.Get<TItem>();

            await this.ApplyIndexInsertionMutationsAsync(async m =>
            {
                foreach (var item in items)
                {
                    await this.AddAsync(item, options, m);
                }
            }).ConfigureAwait(false);
        }

        public async ValueTask AddAsync<TItem>(TItem item)
        {
            var options = this.itemTokenizationOptions.Get<TItem>();

            await this.ApplyIndexInsertionMutationsAsync(async m => await this.AddAsync(item, options, m))
                .ConfigureAwait(false);
        }

        public bool Remove(TKey itemKey)
        {
            if (!this.idPool.Contains(itemKey))
            {
                return false;
            }

            var indexMutation = new IndexRemovalMutation(this.Root, this.IndexNodeFactory);
            var id = this.idPool.ReleaseItem(itemKey);
            this.Root = indexMutation.Remove(id);

            return true;
        }

        public IEnumerable<SearchResult<TKey>> Search(string searchText, TokenizationOptions tokenizationOptions = null)
        {
            var query = this.queryParser.Parse(this.FieldLookup, searchText, this.GetTokenizer(tokenizationOptions));
            return query.Execute(this);
        }

        public override string ToString()
        {
            return this.Root.ToString();
        }

        private void ApplyIndexInsertionMutations(Action<IndexInsertionMutation> mutationAction)
        {
            var indexMutation = new IndexInsertionMutation(this.Root, this.IndexNodeFactory);

            mutationAction(indexMutation);

            this.Root = indexMutation.ApplyInsertions();
        }

        private async Task ApplyIndexInsertionMutationsAsync(Func<IndexInsertionMutation, Task> asyncMutationAction)
        {
            var indexMutation = new IndexInsertionMutation(this.Root, this.IndexNodeFactory);

            await asyncMutationAction(indexMutation).ConfigureAwait(false);

            this.Root = indexMutation.ApplyInsertions();
        }

        private ITokenizer GetTokenizer(TokenizationOptions tokenizationOptions)
        {
            return this.tokenizerFactory.Create(tokenizationOptions ?? this.defaultTokenizationOptions);
        }

        private void Add<TItem>(TItem item, ItemTokenizationOptions<TItem, TKey> options, IndexInsertionMutation indexMutation)
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

        private static void IndexTokens(IndexInsertionMutation indexMutation, int itemId, byte fieldId, IEnumerable<Token> tokens)
        {
            foreach (var word in tokens)
            {
                indexMutation.Add(itemId, fieldId, word);
            }
        }

        private async ValueTask AddAsync<TItem>(TItem item, ItemTokenizationOptions<TItem, TKey> options, IndexInsertionMutation indexMutation)
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
    }
}
