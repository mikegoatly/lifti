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
    internal class StringFieldReader<TObject> : StaticFieldReader<TObject>
    {
        private readonly Func<TObject, string> reader;

        internal StringFieldReader(
            string name,
            Func<TObject, string> reader,
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
            return new ValueTask<IEnumerable<string>>(new[] { this.reader(item) });
        }
    }
}