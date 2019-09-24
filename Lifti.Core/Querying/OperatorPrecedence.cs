namespace Lifti.Querying
{
    /// <summary>
    /// The various levels of operator precedence, in order.
    /// </summary>
    public enum OperatorPrecedence
    {
        /// <summary>
        /// The location-specific statements (near, before, after).
        /// </summary>
        Locational,

        /// <summary>
        /// And statements.
        /// </summary>
        And,

        /// <summary>
        /// Or statements.
        /// </summary>
        Or
    }
}
