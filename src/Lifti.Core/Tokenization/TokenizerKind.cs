namespace Lifti.Tokenization
{
    /// <summary>
    /// The different kinds of tokenizers that are supported out-of-the-box.
    /// </summary>
    public enum TokenizerKind
    {
        /// <summary>
        /// All text provided to the tokenizer will be tokenized.
        /// </summary>
        PlainText = 0,

        /// <summary>
        /// The text provided to the tokenizer will be assumed to contain XML content
        /// of some form, and only element text will be tokenized. Element tags, attributes
        /// and attribute values will not be tokenized.
        /// </summary>
        XmlContent = 1
    }
}
