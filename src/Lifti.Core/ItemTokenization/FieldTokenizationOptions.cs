using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lifti.ItemTokenization
{
    public abstract class FieldTokenizationOptions<TItem> : IFieldTokenizationOptions
    {
        internal FieldTokenizationOptions(string name, TokenizationOptions? tokenizationOptions)
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

        internal abstract ValueTask<IEnumerable<Token>> TokenizeAsync(ITokenizer tokenizer, TItem item);

        internal abstract IEnumerable<Token> Tokenize(ITokenizer tokenizer, TItem item);
    }
}