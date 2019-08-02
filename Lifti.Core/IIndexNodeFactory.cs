namespace Lifti
{
    public interface IIndexNodeFactory : IConfiguredByOptions
    {
        IndexNode CreateNode();
    }
}