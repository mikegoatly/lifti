namespace Lifti
{
    public class IndexNodeFactory : IIndexNodeFactory
    {
        public IndexNode CreateRootNode()
        {
            return new IndexNode(this);
        }

        public IndexNode CreateChildNodeFor(IndexNode parent)
        {
            return new IndexNode(this, parent);
        }
    }
}
