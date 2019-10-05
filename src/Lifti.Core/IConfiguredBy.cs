namespace Lifti
{
    public interface IConfiguredBy<T>
    {
        void Configure(T options);
    }
}
