using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    /// <inheritdoc />
    public abstract class FieldTokenization<TItem> : IFieldTokenization
    {
        internal FieldTokenization(string name, TokenizationOptions? tokenizationOptions)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.TokenizationOptions = tokenizationOptions;
        }

        /// <summary>
        /// Gets the name of the field. This can be referred to when querying to restrict searches to text read for this field only.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the tokenization options to be used when reading tokens for this field.
        /// </summary>
        public TokenizationOptions? TokenizationOptions { get; }

        internal abstract ValueTask<IReadOnlyList<Token>> TokenizeAsync(ITokenizer tokenizer, TItem item);
    }
}