namespace Lifti
{
    public class FullTextIndex<T>
    {
        private readonly IIndexNodeFactory indexNodeFactory = new IndexNodeFactory(); // TODO DI
        private readonly IIdPool<T> idPool = new IdPool<T>();
        private readonly IndexNode root;
        private readonly IWordSplitter splitter = new BasicSplitter();

        public FullTextIndex()
        {
            this.root = this.indexNodeFactory.CreateRootNode();
        }

        public void Index(T item, string text)
        {
            var itemId = this.idPool.CreateIdFor(item);
            foreach (var word in splitter.Process(text))
            {
                root.Index(itemId, word);
            }
        }
    }
}
