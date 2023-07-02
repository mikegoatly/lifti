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
    /// <typeparam name="TItem">
    /// The type of item the field belongs to.
    /// </typeparam>
    internal class StringArrayFieldReader<TItem> : StaticFieldReader<TItem>
    {
        private readonly Func<TItem, IEnumerable<string>> reader;

        internal StringArrayFieldReader(
            string name,
            Func<TItem, IEnumerable<string>> reader,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            IThesaurus thesaurus)
            : base(name, tokenizer, textExtractor, thesaurus)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        /// <inheritdoc />
        public override ValueTask<IEnumerable<string>> ReadAsync(TItem item, CancellationToken cancellationToken)
        {
            return new ValueTask<IEnumerable<string>>(this.reader(item));
        }
    }
}