using Lifti.Tokenization;
using System;

namespace Lifti
{
    /// <summary>
    /// Information about a field that has been configured for indexing.
    /// </summary>
    public struct IndexedFieldDetails : IEquatable<IndexedFieldDetails>
    {
        internal IndexedFieldDetails(byte id, ITokenizer tokenizer)
        {
            this.Id = id;
            this.Tokenizer = tokenizer;
        }

        /// <summary>
        /// The id of the field.
        /// </summary>
        public byte Id { get; }

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
            return other.Id == this.Id && this.Tokenizer == other.Tokenizer;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Id, this.Tokenizer);
        }

        internal void Deconstruct(out byte fieldId, out ITokenizer tokenizer)
        {
            fieldId = this.Id;
            tokenizer = this.Tokenizer;
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
