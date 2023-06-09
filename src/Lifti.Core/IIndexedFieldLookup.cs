using System.Collections.Generic;

namespace Lifti
{
    /// <summary>
    /// Allows for looking up of information about fields configured in the index. Dynamic fields are added to the index at runtime and 
    /// will become available as they are encountered.
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

        /// <summary>
        /// Gets the names of all fields configured in the index, including any dynamic fields that have been registered during the indexing
        /// of objects.
        /// </summary>
        IReadOnlyCollection<string> AllFieldNames { get; }
    }
}