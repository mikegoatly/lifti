using Lifti.Tokenization;
using System;

namespace Lifti
{
    public struct IndexedFieldDetails : IEquatable<IndexedFieldDetails>
    {
        internal IndexedFieldDetails(byte id, ITokenizer tokenizer)
        {
            this.Id = id;
            this.Tokenizer = tokenizer;
        }

        public byte Id { get; }
        public ITokenizer Tokenizer { get; }

        public override bool Equals(object obj)
        {
            return obj is IndexedFieldDetails details &&
                   this.Equals(details);
        }

        public bool Equals(IndexedFieldDetails other)
        {
            return other.Id == this.Id && this.Tokenizer == other.Tokenizer;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Id, this.Tokenizer);
        }

        internal void Deconstruct(out byte fieldId, out ITokenizer tokenizer)
        {
            fieldId = this.Id;
            tokenizer = this.Tokenizer;
        }

        public static bool operator ==(IndexedFieldDetails left, IndexedFieldDetails right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IndexedFieldDetails left, IndexedFieldDetails right)
        {
            return !(left == right);
        }
    }
}
