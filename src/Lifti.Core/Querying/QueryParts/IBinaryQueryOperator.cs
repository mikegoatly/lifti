namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// A <see cref="IQueryPart"/> that operates on two other <see cref="IQueryPart"/>s.
    /// </summary>
    public interface IBinaryQueryOperator : IQueryPart
    {
        /// <summary>
        /// Gets the left side of the operator.
        /// </summary>
        IQueryPart Left { get; set; }

        /// <summary>
        /// Gets the right side of the operator.
        /// </summary>
        IQueryPart Right { get; set; }

        /// <summary>
        /// Gets the precedence for this operator when combined with other <see cref="IQueryPart"/>s.
        /// </summary>
        OperatorPrecedence Precedence { get; }
    }
}
