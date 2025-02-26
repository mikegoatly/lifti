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
    /// <typeparam name="TObject">
    /// The type of object the field belongs to.
    /// </typeparam>
    internal sealed class AsyncFieldReader<TObject> : StaticFieldReader<TObject>
    {
        private readonly Func<TObject, CancellationToken, Task<ReadOnlyMemory<char>>> reader;

        internal AsyncFieldReader(
            string name,
            Func<TObject, CancellationToken, Task<ReadOnlyMemory<char>>> reader,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            IThesaurus thesaurus,
            double scoreBoost)
            : base(name, tokenizer, textExtractor, thesaurus, scoreBoost)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        /// <inheritdoc />
        public override async ValueTask<IEnumerable<ReadOnlyMemory<char>>> ReadAsync(TObject item, CancellationToken cancellationToken)
        {
            return new[] { await this.reader(item, cancellationToken).ConfigureAwait(false) };
        }
    }
}