namespace Lifti
{
    public class IndexNodeFactory : IIndexNodeFactory
    {
        public IndexNode CreateNode()
        {
            return new IndexNode(this);
        }
    }
}
