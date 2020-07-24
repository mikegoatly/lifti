using Lifti.Tokenization;
using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    /// <summary>
    /// A field tokenization capable of asynchronously reading an enumerable of strings for a field.
    /// </summary>
    /// <typeparam name="TItem">
    /// The type of item the field belongs to.
    /// </typeparam>
    internal class AsyncStringArrayFieldReader<TItem> : FieldReader<TItem>
    {
        private readonly Func<TItem, Task<IEnumerable<string>>> reader;

        internal AsyncStringArrayFieldReader(
            string name, 
            Func<TItem, Task<IEnumerable<string>>> reader, 
            ITokenizer? tokenizer,
            ITextExtractor? textExtractor)
            : base(name, tokenizer, textExtractor)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        /// <inheritdoc />
        public override async ValueTask<IEnumerable<string>> ReadAsync(TItem item)
        {
            return await this.reader(item).ConfigureAwait(false);
        }
    }
}