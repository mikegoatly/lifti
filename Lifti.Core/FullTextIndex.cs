using System;
using System.Collections.Generic;

namespace Lifti
{
    public partial class FullTextIndex<T>
    {
        private readonly IIndexNodeFactory indexNodeFactory = new IndexNodeFactory(); // TODO DI
        private readonly IIdPool<T> idPool = new IdPool<T>();
        private readonly IWordSplitter splitter = new BasicSplitter();

        public FullTextIndex()
        {
            this.Root = this.indexNodeFactory.CreateNode();
        }

        public IndexNode Root { get; }

        public void Index(T item, string text)
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

        public IEnumerable<SearchResult<T>> Search(string searchText)
        {
            var searchContext = new SearchContext(this);

            foreach (var searchWord in this.splitter.Process(searchText))
            {
                searchContext.Match(searchWord.Word.AsSpan());
            }

            return searchContext.Results();
        }
    }
}
