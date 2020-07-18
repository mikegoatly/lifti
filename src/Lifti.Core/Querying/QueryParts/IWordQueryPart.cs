namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// Defines a <see cref="IQueryPart"/> that operates on a single string.
    /// </summary>
    public interface IWordQueryPart : IQueryPart
    {
        /// <summary>
        /// Gets the word that is required to be matched by the query.
        /// </summary>
        string Word { get; }
    }
}
