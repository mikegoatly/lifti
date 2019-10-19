using Lifti.Tokenization;
using System;
using System.Collections.Generic;

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

        internal FieldTokenizationOptions(string name, Func<TItem, IEnumerable<string>> reader, TokenizationOptions tokenizationOptions = null)
        {
            this.Name = name;
            this.EnumerableReader = reader;
            this.TokenizationOptions = tokenizationOptions ?? TokenizationOptions.Default;
        }

        /// <summary>
        /// Gets the name of the field. This can be referred to when querying to restrict searches to text read for this field only.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the tokenization options to be used when reading tokens for this field.
        /// </summary>
        public TokenizationOptions TokenizationOptions { get; }

        /// <summary>
        /// Gets the reader capable of reading the entire text for the field, where the text is broken in to multiple segments.
        /// </summary>
        internal Func<TItem, IEnumerable<string>> EnumerableReader { get; }

        /// <summary>
        /// Gets the reader capable of reading the entire text for the field, where the text is a single string.
        /// </summary>
        internal Func<TItem, string> Reader { get; }

        internal IEnumerable<Token> Tokenize(ITokenizer tokenizer, TItem item)
        {
            if (this.Reader != null)
            {
                return tokenizer.Process(this.Reader(item));
            }

            if (this.EnumerableReader != null)
            {
                return tokenizer.Process(this.EnumerableReader(item));
            }

            throw new LiftiException(ExceptionMessages.NoReaderDelegatesConfigured);
        }
    }
}