namespace Lifti
{
    /// <summary>
    /// Allows for looking up of information about fields configured in the index.
    /// </summary>
    public interface IIndexedFieldLookup
    {
        /// <summary>
        /// The id of the default field used when an <see cref="IFullTextIndex{T}.AddAsync(T, string, System.Threading.CancellationToken)"/> overload has been used, 
        /// as opposed to indexing text read from properties of object.
        /// </summary>
        byte DefaultField { get; }

        /// <summary>
        /// Gets the configured name for a field id.
        /// </summary>
        string GetFieldForId(byte id);

        /// <summary>
        /// Gets the configuration required for indexing a named field, including the <see cref="Tokenization.TextExtraction.ITextExtractor"/>
        /// and <see cref="Tokenization.IIndexTokenizer"/> instances to use when processing the field's text.
        /// </summary>
        IndexedFieldDetails GetFieldInfo(string fieldName);
    }
}