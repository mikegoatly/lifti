using Lifti.ItemTokenization;
using Lifti.Querying;
using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Lifti
{
    internal abstract class IndexMutation : IIndexMutation
    {
        private readonly HashSet<IndexNodeMutation> mutatedNodes = new HashSet<IndexNodeMutation>();

        protected IndexMutation(IndexNode root, IIndexNodeFactory indexNodeFactory)
        {
            this.Root = new IndexNodeMutation(0, root, indexNodeFactory);
        }

        protected IndexNodeMutation Root { get; }

        public void TrackMutatedNode(IndexNodeMutation mutatedNode)
        {
            this.mutatedNodes.Add(mutatedNode);
        }

        public IndexNode ApplyMutations()
        {
            return this.Root.Apply();
        }
    }

    internal class IndexRemovalMutation : IndexMutation
    {
        public IndexRemovalMutation(IndexNode root, IIndexNodeFactory indexNodeFactory)
            : base(root, indexNodeFactory)
        {
        }

        internal void Remove(int itemId)
        {
            // TODO
        }
    }

    internal class IndexInsertionMutation : IndexMutation
    {
        public IndexInsertionMutation(IndexNode root, IIndexNodeFactory indexNodeFactory)
            : base(root, indexNodeFactory)
        {
        }

        internal void Index(int itemId, byte fieldId, Token word)
        {
            if (word is null)
            {
                throw new ArgumentNullException(nameof(word));
            }

            Debug.Assert(word.Locations.Select((l, i) => i == 0 || l.WordIndex > word.Locations[i - 1].WordIndex).All(v => v));

            this.Root.Index(itemId, fieldId, word.Locations, word.Value.AsMemory(), this);
        }

        public override string ToString()
        {
            return this.Root.ToString();
        }
    }

    public class FullTextIndex<TKey> : IFullTextIndex<TKey>
    {
        private readonly ITokenizerFactory tokenizerFactory;
        private readonly IQueryParser queryParser;
        private readonly TokenizationOptions defaultTokenizationOptions;
        private readonly ConfiguredItemTokenizationOptions<TKey> itemTokenizationOptions;
        private readonly IdPool<TKey> idPool;

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
            return new IndexNavigator(this.Root);
        }

        public void Add(TKey itemKey, IEnumerable<string> text, TokenizationOptions tokenizationOptions = null)
        {
            // TODO lock for writing on all mutations
            var indexMutation = new IndexInsertionMutation(this.Root, this.IndexNodeFactory);
            var itemId = this.idPool.Add(itemKey);

            var tokenizer = this.GetTokenizer(tokenizationOptions);
            foreach (var word in tokenizer.Process(text))
            {
                indexMutation.Index(itemId, this.FieldLookup.DefaultField, word);
            }

            this.ApplyIndexMutations(indexMutation);
        }

        public void Add(TKey itemKey, string text, TokenizationOptions tokenizationOptions = null)
        {
            var indexMutation = new IndexInsertionMutation(this.Root, this.IndexNodeFactory);
            var itemId = this.idPool.Add(itemKey);

            var tokenizer = this.GetTokenizer(tokenizationOptions);
            foreach (var word in tokenizer.Process(text))
            {
                indexMutation.Index(itemId, this.FieldLookup.DefaultField, word);
            }

            this.ApplyIndexMutations(indexMutation);
        }

        public void AddRange<TItem>(IEnumerable<TItem> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var indexMutation = new IndexInsertionMutation(this.Root, this.IndexNodeFactory);
            var options = this.itemTokenizationOptions.Get<TItem>();

            foreach (var item in items)
            {
                this.Add(item, options, indexMutation);
            }

            this.ApplyIndexMutations(indexMutation);
        }

        public void Add<TItem>(TItem item)
        {
            var options = this.itemTokenizationOptions.Get<TItem>();
            var indexMutation = new IndexInsertionMutation(this.Root, this.IndexNodeFactory);

            this.Add(item, options, indexMutation);
            this.ApplyIndexMutations(indexMutation);
        }

        private void ApplyIndexMutations(IndexMutation indexMutation)
        {
            this.Root = indexMutation.ApplyMutations();
        }

        public async ValueTask AddRangeAsync<TItem>(IEnumerable<TItem> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var options = this.itemTokenizationOptions.Get<TItem>();
            var indexMutation = new IndexInsertionMutation(this.Root, this.IndexNodeFactory);
            foreach (var item in items)
            {
                await this.AddAsync(item, options, indexMutation);
            }

            this.ApplyIndexMutations(indexMutation);
        }

        public async ValueTask AddAsync<TItem>(TItem item)
        {
            var options = this.itemTokenizationOptions.Get<TItem>();
            var indexMutation = new IndexInsertionMutation(this.Root, this.IndexNodeFactory);

            await this.AddAsync(item, options, indexMutation);

            this.ApplyIndexMutations(indexMutation);
        }

        public bool Remove(TKey itemKey)
        {
            if (!this.idPool.Contains(itemKey))
            {
                return false;
            }

            var indexMutation = new IndexRemovalMutation(this.Root, this.IndexNodeFactory);
            var id = this.idPool.ReleaseItem(itemKey);
            indexMutation.Remove(id);
            this.ApplyIndexMutations(indexMutation);

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
                indexMutation.Index(itemId, fieldId, word);
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
