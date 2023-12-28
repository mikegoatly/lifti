namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// Manages context during the execution of a query, allowing for aspects like field filters to be appled.
    /// </summary>
    public interface IQueryContext
    {
        /// <summary>
        /// Applies any additional filters present in the current query context, e.g. field filters, 
        /// to the given <see cref="IntermediateQueryResult"/>, returning a new <see cref="IntermediateQueryResult"/> instance.
        /// </summary>
        IntermediateQueryResult ApplyTo(IntermediateQueryResult intermediateQueryResult);

        /// <summary>
        /// Applies any additional filters present in the current query context, e.g. field filters,
        /// to the given <see cref="MatchCollector"/>, which is mutated as required.
        /// </summary>
        void ApplyTo(MatchCollector matchCollector);
    }
}
