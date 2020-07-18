namespace Lifti.Querying
{
    /// <summary>
    /// Exposes methods for creating an <see cref="IScorer"/> for an <see cref="IIndexSnapshot"/>.
    /// </summary>
    public interface IIndexScorerFactory
    {
        /// <summary>
        /// Creates a scorer for the given <see cref="IIndexSnapshot"/>.
        /// </summary>
        IScorer CreateIndexScorer(IIndexSnapshot indexSnapshot);
    }
}
