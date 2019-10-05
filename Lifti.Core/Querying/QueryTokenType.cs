namespace Lifti.Querying
{
    /// <summary>
    /// The various types of query tokens that can be encountered.
    /// </summary>
    public enum QueryTokenType
    {
        /// <summary>
        /// The token contains text to search on.
        /// </summary>
        Text,

        /// <summary>
        /// The token is an And operator.
        /// </summary>
        AndOperator,

        /// <summary>
        /// The token is an Or operator.
        /// </summary>
        OrOperator,

        /// <summary>
        /// The token is an open bracket.
        /// </summary>
        OpenBracket,

        /// <summary>
        /// The token is a close bracket.
        /// </summary>
        CloseBracket,

        /// <summary>
        /// The token indicates that all subsequent text tokens should appear immediately next to each other.
        /// </summary>
        BeginAdjacentTextOperator,

        /// <summary>
        /// The token indicates an end to adjacent text tokens.
        /// </summary>
        EndAdjacentTextOperator,

        /// <summary>
        /// The token is a Near operator - the words must be near each other in either direction.
        /// </summary>
        NearOperator,

        /// <summary>
        /// The token is a Preceding Near operator - the left word must be near and precede the right word.
        /// </summary>
        PrecedingNearOperator,

        /// <summary>
        /// The token is a preceding operator - the left word must precede the right word.
        /// </summary>
        PrecedingOperator,

        /// <summary>
        /// The token is a field filter - the captured word is the field that results should be restricted to.
        /// </summary>
        FieldFilter
    }

}
