namespace Lifti.Querying
{
    internal interface IIndexNavigatorPool
    {
        IIndexNavigator Create(IIndexSnapshot snapshot);
        void Return(IndexNavigator indexNavigator);
    }
}