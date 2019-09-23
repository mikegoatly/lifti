using Lifti.Tokenization;
using System;
using System.Collections.Generic;

namespace Lifti
{

    public partial class FullTextIndex<TKey>
    {
        private readonly IIndexNodeFactory indexNodeFactory;
        private readonly ITokenizerFactory tokenizerFactory;
        private readonly IIdPool<TKey> idPool = new IdPool<TKey>();
        private readonly IndexedFieldLookup fieldLookup = new IndexedFieldLookup();

        public FullTextIndex()
            : this(new FullTextIndexOptions<TKey>())
        {
        }

        public FullTextIndex(
            FullTextIndexOptions<TKey> options,
            IIndexNodeFactory indexNodeFactory = default,
            ITokenizerFactory tokenizerFactory = default)
        {
            this.indexNodeFactory = indexNodeFactory ?? new IndexNodeFactory();
            this.tokenizerFactory = tokenizerFactory ?? new TokenizerFactory();

            this.indexNodeFactory.Configure(options);

            this.Root = this.indexNodeFactory.CreateNode();
        }

        public IndexNode Root { get; }

        public void Index(TKey itemKey, string text, TokenizationOptions? tokenizationOptions = default)
        {
            var itemId = this.idPool.CreateIdFor(itemKey);

            var tokenizer = this.tokenizerFactory.Create(tokenizationOptions ?? TokenizationOptions.Default);
            foreach (var word in tokenizer.Process(text))
            {
                this.Root.Index(itemId, this.fieldLookup.DefaultField, word);
            }
        }

        public void Index<TItem>(TItem item, ItemTokenizationOptions<TItem, TKey> itemTokenizationOptions)
        {
            var itemKey = itemTokenizationOptions.KeyReader(item);
            var itemId = this.idPool.CreateIdFor(itemKey);

            foreach (var field in itemTokenizationOptions.FieldTokenization)
            {
                var fieldId = this.fieldLookup.GetOrCreateIdForField(field.Name);
                var tokenizer = this.tokenizerFactory.Create(field.TokenizationOptions);
                foreach (var word in tokenizer.Process(field.Reader(item)))
                {
                    this.Root.Index(itemId, fieldId, word);
                }
            }
        }

        public IEnumerable<SearchResult<TKey>> Search(string searchText, TokenizationOptions? tokenizationOptions = default)
        {
            var searchContext = new SearchContext(this);

            var tokenizer = this.tokenizerFactory.Create(tokenizationOptions ?? TokenizationOptions.Default);
            foreach (var searchWord in tokenizer.Process(searchText))
            {
                searchContext.Match(searchWord.Value.AsSpan());
            }

            return searchContext.Results();
        }
    }
}
