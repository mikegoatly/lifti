using Lifti.Querying;
using Lifti.Tokenization;
using System;
using System.Collections.Generic;

namespace Lifti
{
    public partial class FullTextIndex<TKey> : IFullTextIndex<TKey>
    {
        private readonly IIndexNodeFactory indexNodeFactory;
        private readonly ITokenizerFactory tokenizerFactory;
        private readonly IQueryParser queryParser;

        public FullTextIndex()
            : this(new FullTextIndexConfiguration<TKey>())
        {
        }

        public FullTextIndex(
            FullTextIndexConfiguration<TKey> options,
            IIndexNodeFactory indexNodeFactory = null,
            ITokenizerFactory tokenizerFactory = null,
            IQueryParser queryParser = null)
        {
            this.indexNodeFactory = indexNodeFactory ?? new IndexNodeFactory();
            this.tokenizerFactory = tokenizerFactory ?? new TokenizerFactory();
            this.queryParser = queryParser ?? new QueryParser();

            this.indexNodeFactory.Configure(options);

            this.IdPool = new IdPool<TKey>();
            this.FieldLookup = new IndexedFieldLookup();
            this.Root = this.indexNodeFactory.CreateNode();
        }

        public IndexNode Root { get; }

        public IIdPool<TKey> IdPool { get; }

        public IIndexedFieldLookup FieldLookup { get; }

        public int Count => this.IdPool.AllocatedIdCount;

        public IndexNavigator CreateNavigator()
        {
            return new IndexNavigator(this.Root);
        }

        public void Index(TKey itemKey, string text, TokenizationOptions? tokenizationOptions = null)
        {
            var itemId = this.IdPool.CreateIdFor(itemKey);

            var tokenizer = GetTokenizer(tokenizationOptions);
            foreach (var word in tokenizer.Process(text))
            {
                this.Root.Index(itemId, this.FieldLookup.DefaultField, word);
            }
        }

        public void Index<TItem>(TItem item, ItemTokenizationOptions<TItem, TKey> itemTokenizationOptions)
        {
            if (itemTokenizationOptions is null)
            {
                throw new ArgumentNullException(nameof(itemTokenizationOptions));
            }

            var itemKey = itemTokenizationOptions.KeyReader(item);
            var itemId = this.IdPool.CreateIdFor(itemKey);

            foreach (var field in itemTokenizationOptions.FieldTokenization)
            {
                var fieldId = this.FieldLookup.GetOrCreateIdForField(field.Name);
                var tokenizer = this.tokenizerFactory.Create(field.TokenizationOptions);
                foreach (var word in tokenizer.Process(field.Reader(item)))
                {
                    this.Root.Index(itemId, fieldId, word);
                }
            }
        }

        public IEnumerable<SearchResult<TKey>> Search(string searchText, TokenizationOptions? tokenizationOptions = null)
        {
            var query = this.queryParser.Parse(this.FieldLookup, searchText, this.GetTokenizer(tokenizationOptions));
            return query.Execute(this);
        }

        private ITokenizer GetTokenizer(TokenizationOptions? tokenizationOptions)
        {
            return this.tokenizerFactory.Create(tokenizationOptions ?? TokenizationOptions.Default);
        }
    }
}
