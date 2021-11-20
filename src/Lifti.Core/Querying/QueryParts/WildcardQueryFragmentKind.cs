namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// The various kinds of fragments representable by <see cref="WildcardQueryFragment"/>.
    /// </summary>
    public enum WildcardQueryFragmentKind
    {
        /// <summary>
        /// The fragment represents a specific piece of text that must be matched
        /// </summary>
        Text = 0,

        /// <summary>
        /// The fragment represents any single substitute character at the location in the search text.
        /// </summary>
        SingleCharacter = 1,

        /// <summary>
        /// The fragment represents any number of characters at the location in the search text
        /// </summary>
        MultiCharacter = 2
    }
}
