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
    internal class StringArrayFieldReader<TObject> : StaticFieldReader<TObject>
    {
        private readonly Func<TObject, IEnumerable<string>> reader;

        internal StringArrayFieldReader(
            string name,
            Func<TObject, IEnumerable<string>> reader,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            IThesaurus thesaurus,
            double scoreBoost)
            : base(name, tokenizer, textExtractor, thesaurus, scoreBoost)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        /// <inheritdoc />
        public override ValueTask<IEnumerable<string>> ReadAsync(TObject item, CancellationToken cancellationToken)
        {
            return new ValueTask<IEnumerable<string>>(this.reader(item));
        }
    }
}