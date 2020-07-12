namespace Lifti
{
    /// <summary>
    /// Allows for looking up of information about fields configured in the index.
    /// </summary>
    public interface IIndexedFieldLookup
    {
        /// <summary>
        /// The id of the default field used when an <see cref="IFullTextIndex.AddAsync"/> overload has been used with strings, 
        /// as opposed to indexing text read from properties of object.
        /// </summary>
        byte DefaultField { get; }

        /// <summary>
        /// Gets the configured name for a field id.
        /// </summary>
        string GetFieldForId(byte id);

        /// <summary>
        /// Gets the configuration required for indexing a named field, including the <see cref="Lifti.Tokenization.ITokenizer"/>
        /// instance to use.
        /// </summary>
        IndexedFieldDetails GetFieldInfo(string fieldName);
    }
}