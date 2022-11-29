using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    /// <summary>
    /// A field tokenization capable of asynchronously reading s string for a field.
    /// </summary>
    /// <typeparam name="TItem">
    /// The type of item the field belongs to.
    /// </typeparam>
    internal class StringFieldReader<TItem> : FieldReader<TItem>
    {
        private readonly Func<TItem, string> reader;

        internal StringFieldReader(
            string name,
            Func<TItem, string> reader,
            IIndexTokenizer? tokenizer,
            ITextExtractor? textExtractor)
            : base(name, tokenizer, textExtractor)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        /// <inheritdoc />
        public override ValueTask<IEnumerable<string>> ReadAsync(TItem item)
        {
            return new ValueTask<IEnumerable<string>>(new[] { this.reader(item) });
        }
    }
}