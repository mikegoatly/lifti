using Lifti.Preprocessing;
using System;
using System.Collections.Generic;

namespace Lifti
{
    public partial class FullTextIndex<TKey>
    {
        private readonly IIndexNodeFactory indexNodeFactory;
        private readonly IIdPool<TKey> idPool = new IdPool<TKey>();
        private readonly ITokenizer splitter;

        public FullTextIndex()
            : this(new FullTextIndexOptions<TKey>())
        {
        }

        public FullTextIndex(FullTextIndexOptions<TKey> options)
            : this(
                  options,
                  new BasicTokenizer(new InputPreprocessorPipeline(Array.Empty<IInputPreprocessor>())),
                  new IndexNodeFactory())
        {
        }

        public FullTextIndex(
            FullTextIndexOptions<TKey> options,
            ITokenizer wordSplitter,
            IIndexNodeFactory indexNodeFactory)
        {
            this.ConfigureWith(options, wordSplitter, indexNodeFactory);
            this.splitter = wordSplitter;
            this.indexNodeFactory = indexNodeFactory;
            this.Root = this.indexNodeFactory.CreateNode();
        }

        public IndexNode Root { get; }

        public void Index(TKey item, string text)
        {
            var itemId = this.idPool.CreateIdFor(item);
            foreach (var word in this.splitter.Process(text))
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

        public IEnumerable<SearchResult<TKey>> Search(string searchText)
        {
            var searchContext = new SearchContext(this);

            foreach (var searchWord in this.splitter.Process(searchText))
            {
                searchContext.Match(searchWord.Value.AsSpan());
            }

            return searchContext.Results();
        }

        private void ConfigureWith(FullTextIndexOptions<TKey> options, params IConfiguredByOptions[] configurable)
        {
            foreach (var item in configurable)
            {
                item.ConfigureWith(options);
            }
        }
    }
}
