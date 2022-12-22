﻿using Lifti.Tokenization;
using Lifti.Tokenization.TextExtraction;
using System;

namespace Lifti
{
    /// <summary>
    /// Information about a field that has been configured for indexing.
    /// </summary>
    public struct IndexedFieldDetails : IEquatable<IndexedFieldDetails>
    {
        internal IndexedFieldDetails(byte id, ITextExtractor textExtractor, IIndexTokenizer tokenizer, IThesaurus thesaurus)
        {
            this.Id = id;
            this.TextExtractor = textExtractor;
            this.Tokenizer = tokenizer;
            this.Thesaurus = thesaurus;
        }

        /// <summary>
        /// The id of the field.
        /// </summary>
        public byte Id { get; }

        /// <summary>
        /// Gets the <see cref="ITextExtractor"/> used to extract sections of text from this field.
        /// </summary>
        public ITextExtractor TextExtractor { get; }

        /// <summary>
        /// The <see cref="IIndexTokenizer"/> that should be used when tokenizing text for the field.
        /// </summary>
        public IIndexTokenizer Tokenizer { get; }

        /// <summary>
        /// The <see cref="IThesaurus"/> that should be used to expand tokens when processing text for this field.
        /// </summary>
        public IThesaurus Thesaurus { get; }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is IndexedFieldDetails details &&
                   this.Equals(details);
        }

        /// <inheritdoc />
        public bool Equals(IndexedFieldDetails other)
        {
            return other.Id == this.Id &&
                this.Tokenizer == other.Tokenizer &&
                this.TextExtractor == other.TextExtractor &&
                this.Thesaurus == other.Thesaurus;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Id, this.Tokenizer, this.TextExtractor);
        }

        internal void Deconstruct(out byte fieldId, out ITextExtractor textExtractor, out IIndexTokenizer tokenizer, out IThesaurus thesaurus)
        {
            fieldId = this.Id;
            tokenizer = this.Tokenizer;
            textExtractor = this.TextExtractor;
            thesaurus = this.Thesaurus;
        }

        /// <inheritdoc />
        public static bool operator ==(IndexedFieldDetails left, IndexedFieldDetails right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc />
        public static bool operator !=(IndexedFieldDetails left, IndexedFieldDetails right)
        {
            return !(left == right);
        }
    }
}
