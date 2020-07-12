namespace Lifti.Querying.QueryParts
{
    public interface IWordQueryPart : IQueryPart
    {
        /// <summary>
        /// Gets the word that is required to be matched by the query.
        /// </summary>
        string Word { get; }
    }
}
