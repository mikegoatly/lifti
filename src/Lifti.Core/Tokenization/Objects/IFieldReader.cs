﻿using Lifti.Tokenization.TextExtraction;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    /// <summary>
    /// Provides an implementation by which an object's field can be read.
    /// </summary>
    internal interface IFieldReader
    {
        /// <summary>
        /// Gets the name of the field. This can be referred to when querying to restrict searches to text read for this field only.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the <see cref="ITokenizer"/> to be used for this field. If this is null then the default tokenizer for the index will be used.
        /// </summary>
        ITokenizer? Tokenizer { get; }

        /// <summary>
        /// Gets the <see cref="ITextExtractor"/> to be used for this field. If this is null then the default text extractor for the index will be used.
        /// </summary>
        ITextExtractor? TextExtractor { get; }
    }

    internal interface IFieldReader<TItem> : IFieldReader
    {
        /// <summary>
        /// Reads the field's text fromt he given item.
        /// </summary>
        ValueTask<IEnumerable<string>> ReadAsync(TItem item);
    }
}