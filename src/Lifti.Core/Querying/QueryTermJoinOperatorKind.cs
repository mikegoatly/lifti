namespace Lifti.Querying
{
    /// <summary>
    /// Used to instructs the <see cref="SimpleQueryParser"/> which joining operator should be used to combine search terms.
    /// </summary>
    public enum QueryTermJoinOperatorKind
    {
        /// <summary>
        /// Search terms will be joined with an "and", requiring that all of them are present in a document for it to be matched.
        /// </summary>
        And = 0,

        /// <summary>
        /// Search terms will be joined with an "or", requiring that any one of them are present in a document for it to be matched.
        /// </summary>
        Or = 1
    }
}
