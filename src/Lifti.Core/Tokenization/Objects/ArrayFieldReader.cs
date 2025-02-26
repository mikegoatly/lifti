using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    /// <summary>
    /// A field tokenization capable of reading an enumerable of strings for a field.
    /// </summary>
    /// <typeparam name="TObject">
    /// The type of item the field belongs to.
    /// </typeparam>
    internal sealed class ArrayFieldReader<TObject> : StaticFieldReader<TObject>
    {
        private readonly Func<TObject, IEnumerable<ReadOnlyMemory<char>>> reader;

        internal ArrayFieldReader(
            string name,
            Func<TObject, IEnumerable<ReadOnlyMemory<char>>> reader,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            IThesaurus thesaurus,
            double scoreBoost)
            : base(name, tokenizer, textExtractor, thesaurus, scoreBoost)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        /// <inheritdoc />
        public override ValueTask<IEnumerable<ReadOnlyMemory<char>>> ReadAsync(TObject item, CancellationToken cancellationToken)
        {
            return new ValueTask<IEnumerable<ReadOnlyMemory<char>>>(this.reader(item));
        }
    }
}