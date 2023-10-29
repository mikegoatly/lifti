using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    /// <summary>
    /// A field tokenization capable of asynchronously reading a string for a field.
    /// </summary>
    /// <typeparam name="TItem">
    /// The type of item the field belongs to.
    /// </typeparam>
    internal class AsyncStringFieldReader<TItem> : StaticFieldReader<TItem>
    {
        private readonly Func<TItem, CancellationToken, Task<string>> reader;

        internal AsyncStringFieldReader(
            string name,
            Func<TItem, CancellationToken, Task<string>> reader,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            IThesaurus thesaurus,
            double scoreBoost)
            : base(name, tokenizer, textExtractor, thesaurus, scoreBoost)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        /// <inheritdoc />
        public override async ValueTask<IEnumerable<string>> ReadAsync(TItem item, CancellationToken cancellationToken)
        {
            return new[] { await this.reader(item, cancellationToken).ConfigureAwait(false) };
        }
    }
}