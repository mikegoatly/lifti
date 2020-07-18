namespace Lifti
{
    /// <summary>
    /// Describes the sort of indexing that should occur in an index. This will 
    /// vary depending on depth of a node in the index and the index configuration.
    /// </summary>
    public enum IndexSupportLevelKind
    {
        /// <summary>
        /// Intra-node text should not form part of the index structure. 
        /// </summary>
        CharacterByCharacter = 0,

        /// <summary>
        /// Intra-node text is allowed at this point in the index.
        /// </summary>
        IntraNodeText = 1
    }
}
