namespace Lifti
{
    public interface IIndexNodeFactory : IConfiguredByOptions
    {
        IndexNode CreateNode();
        IndexNode CreateNode(IndexNode parent);
    }
}