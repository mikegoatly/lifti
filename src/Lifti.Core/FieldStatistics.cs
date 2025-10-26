namespace Lifti
{
    /// <summary>
    /// Statistics for a specific field within an indexed document.
    /// </summary>
    /// <param name="TokenCount">The number of tokens indexed in this field.</param>
    /// <param name="LastTokenIndex">
    /// The last (maximum) token index in this field. This is used for exact match queries
    /// (using &lt;&lt; and &gt;&gt; operators) to determine if a match occurs at the start or end of a field.
    /// If there are no tokens in the field, this value is -1.
    /// </param>
    public readonly record struct FieldStatistics(int TokenCount, int LastTokenIndex);
}
