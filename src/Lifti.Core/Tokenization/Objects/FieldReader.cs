﻿using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    /// <inheritdoc />
    internal abstract class FieldReader<TItem> : IFieldReader<TItem>
    {
        internal FieldReader(string name, IIndexTokenizer tokenizer, ITextExtractor textExtractor, IThesaurus thesaurus)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Tokenizer = tokenizer;
            this.TextExtractor = textExtractor;
            this.Thesaurus = thesaurus;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public IIndexTokenizer Tokenizer { get; }

        /// <inheritdoc />
        public ITextExtractor TextExtractor { get; }

        /// <inheritdoc />
        public IThesaurus Thesaurus { get; }

        /// <inheritdoc />
        public abstract ValueTask<IEnumerable<string>> ReadAsync(TItem item, CancellationToken cancellationToken);
    }
}