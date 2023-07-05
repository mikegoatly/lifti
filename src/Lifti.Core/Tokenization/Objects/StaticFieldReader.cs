using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    /// <inheritdoc />
    internal abstract class StaticFieldReader<TItem> : FieldConfig, IStaticFieldReader<TItem>
    {
        internal StaticFieldReader(string name, IIndexTokenizer tokenizer, ITextExtractor textExtractor, IThesaurus thesaurus)
            : base(tokenizer, textExtractor, thesaurus)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public abstract ValueTask<IEnumerable<string>> ReadAsync(TItem item, CancellationToken cancellationToken);
    }
}