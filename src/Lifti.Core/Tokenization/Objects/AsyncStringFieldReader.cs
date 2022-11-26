using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    /// <summary>
    /// A field tokenization capable of asynchronously reading a string for a field.
    /// </summary>
    /// <typeparam name="TItem">
    /// The type of item the field belongs to.
    /// </typeparam>
    internal class AsyncStringFieldReader<TItem> : FieldReader<TItem>
    {
        private readonly Func<TItem, Task<string>> reader;

        internal AsyncStringFieldReader(
            string name,
            Func<TItem, Task<string>> reader,
            IIndexTokenizer? tokenizer,
            ITextExtractor? textExtractor)
            : base(name, tokenizer, textExtractor)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        /// <inheritdoc />
        public override async ValueTask<IEnumerable<string>> ReadAsync(TItem item)
        {
            return new[] { await this.reader(item).ConfigureAwait(false) };
        }
    }
}