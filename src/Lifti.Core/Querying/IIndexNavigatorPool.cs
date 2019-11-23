namespace Lifti.Querying
{
    internal interface IIndexNavigatorPool
    {
        IIndexNavigator Create(IndexNode node);
        void Return(IndexNavigator indexNavigator);
    }
}