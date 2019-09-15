namespace Lifti
{
    public interface IIndexNodeFactory : IConfiguredBy<FullTextIndexConfiguration>
    {
        IndexNode CreateNode();
        IndexNode CreateNode(IndexNode parent);
    }
}