using System;

namespace Lifti
{
    public class FieldTokenizationOptions<TItem>
    {
        internal FieldTokenizationOptions(string name, Func<TItem, string> reader, TokenizationOptions tokenizationOptions = null)
        {
            this.Name = name;
            this.Reader = reader;
            this.TokenizationOptions = tokenizationOptions ?? TokenizationOptions.Default;
        }

        /// <summary>
        /// Gets the name of the field. This can be referred to when querying to restrict searches to text read for this field only.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the reader capable of reading the entire text for the field.
        /// </summary>
        internal Func<TItem, string> Reader { get; }

        /// <summary>
        /// Gets the tokenization options to be used when reading tokens for this field.
        /// </summary>
        public TokenizationOptions TokenizationOptions { get; }
    }
}