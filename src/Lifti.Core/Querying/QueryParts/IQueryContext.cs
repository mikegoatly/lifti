namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// Manages context during the execution of a query, allowing for aspects like field filters to be appled.
    /// </summary>
    public interface IQueryContext
    {
        IntermediateQueryResult ApplyTo(IntermediateQueryResult intermediateQueryResult);
    }
}
