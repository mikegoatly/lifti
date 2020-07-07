namespace Lifti.Querying
{
    public interface IScorer
    {
        IIndexScorer CreateIndexScorer(IIndexSnapshot indexSnapshot);
    }
}
