using Lifti.Preprocessing;
using System;
using System.Collections.Generic;

namespace Lifti
{
    public partial class FullTextIndex<TKey>
    {
        private readonly IIndexNodeFactory indexNodeFactory;
        private readonly ITokenizerFactory tokenizerFactory;
        private readonly IIdPool<TKey> idPool = new IdPool<TKey>();

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

        public void Index(TKey item, string text, TokenizationOptions? tokenizationOptions = default)
        {
            var itemId = this.idPool.CreateIdFor(item);

            var tokenizer = this.tokenizerFactory.Create(tokenizationOptions ?? TokenizationOptions.Default);
            foreach (var word in tokenizer.Process(text))
            {
                this.Root.Index(itemId, 0, word);
            }
        }

        private string GetFieldName(byte fieldId)
        {
            if (fieldId != 0)
            {
                throw new NotImplementedException("Ultimately indexing an object by multiple fields will be possible - this will return the name of the field that the text was found in");
            }

            return string.Empty;
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
