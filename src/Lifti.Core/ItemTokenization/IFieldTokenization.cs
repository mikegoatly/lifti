namespace Lifti.ItemTokenization
{
    internal interface IFieldTokenization
    {
        /// <summary>
        /// Gets the name of the field. This can be referred to when querying to restrict searches to text read for this field only.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the tokenization options to be used when reading tokens for this field.
        /// </summary>
        TokenizationOptions? TokenizationOptions { get; }
    }
}