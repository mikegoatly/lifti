﻿using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    /// <inheritdoc />
    internal abstract class StaticFieldReader<TObject> : FieldConfig, IStaticFieldReader<TObject>
    {
        internal StaticFieldReader(string name, IIndexTokenizer tokenizer, ITextExtractor textExtractor, IThesaurus thesaurus, double scoreBoost)
            : base(tokenizer, textExtractor, thesaurus, scoreBoost)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public abstract ValueTask<IEnumerable<string>> ReadAsync(TObject item, CancellationToken cancellationToken);
    }
}