using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <typeparam name="TNestedObject">
    /// The type of the nested object being read from <typeparamref name="TItem"/>.
    /// </typeparam>
    internal class NestedObjectFieldReader<TItem, TNestedObject> : FieldReader<TItem>
    {
        private readonly Func<TItem, IEnumerable<TNestedObject>> reader;
        private readonly Func<TNestedObject, string> nestedObjectTextReader;

        internal NestedObjectFieldReader(
            string name,
            Func<TItem, IEnumerable<TNestedObject>> reader,
            Func<TNestedObject, string> nestedObjectTextReader,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            IThesaurus thesaurus)
            : base(name, tokenizer, textExtractor, thesaurus)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
            this.nestedObjectTextReader = nestedObjectTextReader ?? throw new ArgumentNullException(nameof(nestedObjectTextReader));
        }

        /// <inheritdoc />
        public override ValueTask<IEnumerable<string>> ReadAsync(TItem item, CancellationToken cancellationToken)
        {
            return new ValueTask<IEnumerable<string>>(this.reader(item).Select(nestedObjectTextReader));
        }
    }
}