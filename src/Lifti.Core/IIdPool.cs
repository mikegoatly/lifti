namespace Lifti
{
    public interface IIdPool<T>
    {
        int AllocatedIdCount { get; }

        int CreateIdFor(T item);
        T GetItemForId(int id);
        int ReleaseItem(T item);
    }
}