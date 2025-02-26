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
    internal sealed class TextFieldReader<TObject> : StaticFieldReader<TObject>
    {
        private readonly Func<TObject, ReadOnlyMemory<char>> reader;

        internal TextFieldReader(
            string name,
            Func<TObject, ReadOnlyMemory<char>> reader,
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
            return new ValueTask<IEnumerable<ReadOnlyMemory<char>>>(new[] { this.reader(item) });
        }
    }
}