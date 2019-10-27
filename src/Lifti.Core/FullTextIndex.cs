using Lifti.Querying;
using Lifti.Tokenization;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Lifti
{
    public class FullTextIndex<TKey> : IFullTextIndex<TKey>
    {
        private readonly IIndexNodeFactory indexNodeFactory;
        private readonly ITokenizerFactory tokenizerFactory;
        private readonly IQueryParser queryParser;
        private readonly ConfiguredItemTokenizationOptions<TKey> itemTokenizationOptions;

        internal FullTextIndex(
            ConfiguredItemTokenizationOptions<TKey> itemTokenizationOptions,
            IIndexNodeFactory indexNodeFactory,
            ITokenizerFactory tokenizerFactory,
            IQueryParser queryParser)
        {
            this.itemTokenizationOptions = itemTokenizationOptions ?? throw new ArgumentNullException(nameof(itemTokenizationOptions));
            this.indexNodeFactory = indexNodeFactory ?? throw new ArgumentNullException(nameof(indexNodeFactory));
            this.tokenizerFactory = tokenizerFactory ?? throw new ArgumentNullException(nameof(tokenizerFactory));
            this.queryParser = queryParser ?? throw new ArgumentNullException(nameof(queryParser));

            this.IdPool = new IdPool<TKey>();
            this.FieldLookup = new IndexedFieldLookup();
            this.Root = this.indexNodeFactory.CreateNode();
        }

        public IndexNode Root { get; }

        public IIdPool<TKey> IdPool { get; }

        public IIndexedFieldLookup FieldLookup { get; }

        public int Count => this.IdPool.AllocatedIdCount;

        public IIndexNavigator CreateNavigator()
        {
            return new IndexNavigator(this.Root);
        }

        public void Add(TKey itemKey, string text, TokenizationOptions tokenizationOptions = null)
        {
            var itemId = this.IdPool.CreateIdFor(itemKey);

            var tokenizer = this.GetTokenizer(tokenizationOptions);
            foreach (var word in tokenizer.Process(text))
            {
                this.Root.Index(itemId, this.FieldLookup.DefaultField, word);
            }
        }

        public void AddRange<TItem>(IEnumerable<TItem> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach (var item in items)
            {
                this.Add(item);
            }
        }

        public void Add<TItem>(TItem item)
        {
            var options = this.itemTokenizationOptions.Get<TItem>();

            var itemKey = options.KeyReader(item);
            var itemId = this.IdPool.CreateIdFor(itemKey);

            foreach (var field in options.FieldTokenization)
            {
                var fieldId = this.FieldLookup.GetOrCreateIdForField(field.Name);
                var tokenizer = this.tokenizerFactory.Create(field.TokenizationOptions);
                var tokens = field.Tokenize(tokenizer, item);

                foreach (var word in tokens)
                {
                    this.Root.Index(itemId, fieldId, word);
                }
            }
        }

        public bool Remove(TKey itemKey)
        {
            if (!this.IdPool.Contains(itemKey))
            {
                return false;
            }

            var id = this.IdPool.ReleaseItem(itemKey);
            this.Root.Remove(id);

            return true;
        }

        public IEnumerable<SearchResult<TKey>> Search(string searchText, TokenizationOptions tokenizationOptions = null)
        {
            var query = this.queryParser.Parse(this.FieldLookup, searchText, this.GetTokenizer(tokenizationOptions));
            return query.Execute(this);
        }

        private ITokenizer GetTokenizer(TokenizationOptions tokenizationOptions)
        {
            return this.tokenizerFactory.Create(tokenizationOptions ?? TokenizationOptions.Default);
        }
    }
}
