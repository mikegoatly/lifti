using Lifti.Tokenization;
using Lifti.Tokenization.TextExtraction;
using System;

namespace Lifti
{
    /// <summary>
    /// Information about a field that has been configured for indexing.
    /// </summary>
    public struct IndexedFieldDetails : IEquatable<IndexedFieldDetails>
    {
        internal IndexedFieldDetails(byte id, ITextExtractor textExtractor, ITokenizer tokenizer)
        {
            this.Id = id;
            this.TextExtractor = textExtractor;
            this.Tokenizer = tokenizer;
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
        /// The <see cref="ITokenizer"/> that should be used when tokenizing text for the field.
        /// </summary>
        public ITokenizer Tokenizer { get; }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is IndexedFieldDetails details &&
                   this.Equals(details);
        }

        /// <inheritdoc />
        public bool Equals(IndexedFieldDetails other)
        {
            return other.Id == this.Id && 
                this.Tokenizer == other.Tokenizer &&
                this.TextExtractor == other.TextExtractor;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Id, this.Tokenizer, this.TextExtractor);
        }

        internal void Deconstruct(out byte fieldId, out ITextExtractor textExtractor, out ITokenizer tokenizer)
        {
            fieldId = this.Id;
            tokenizer = this.Tokenizer;
            textExtractor = this.TextExtractor;
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
