namespace Lifti
{
    public interface IIndexNodeFactory : IConfiguredBy<AdvancedOptions>
    {
        IndexNode CreateNode();
        IndexNode CreateNode(IndexNode parent);
    }
}