namespace Lifti
{
    public interface IIndexNodeFactory
    {
        IndexNode CreateChildNodeFor(IndexNode parent);
        IndexNode CreateRootNode();
    }
}