using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading;
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
        private readonly Func<TItem, CancellationToken, Task<IEnumerable<string>>> reader;

        internal AsyncStringArrayFieldReader(
            string name,
            Func<TItem, CancellationToken, Task<IEnumerable<string>>> reader,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            IThesaurus thesaurus)
            : base(name, tokenizer, textExtractor, thesaurus)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        /// <inheritdoc />
        public override async ValueTask<IEnumerable<string>> ReadAsync(TItem item, CancellationToken cancellationToken)
        {
            return await this.reader(item, cancellationToken).ConfigureAwait(false);
        }
    }
}