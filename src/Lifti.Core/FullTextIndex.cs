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
        private readonly ConfiguredItemTokenizationOptions<TKey> itemTokenizationOptions = new ConfiguredItemTokenizationOptions<TKey>();

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

        public IIndexNavigator CreateNavigator()
        {
            return new IndexNavigator(this.Root);
        }

        public ItemTokenizationOptions<TItem, TKey> WithItemTokenization<TItem>(Func<TItem, TKey> idReader)
        {
            var options = new ItemTokenizationOptions<TItem, TKey>(idReader);
            this.itemTokenizationOptions.Add(options);
            return options;
        }

        public void Index(TKey itemKey, string text, TokenizationOptions tokenizationOptions = null)
        {
            var itemId = this.IdPool.CreateIdFor(itemKey);

            var tokenizer = this.GetTokenizer(tokenizationOptions);
            foreach (var word in tokenizer.Process(text))
            {
                this.Root.Index(itemId, this.FieldLookup.DefaultField, word);
            }
        }

        public void Index<TItem>(TItem item)
        {
            var options = itemTokenizationOptions.Get<TItem>();

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
