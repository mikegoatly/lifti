using System;

namespace Lifti
{
    public struct IndexedItem : IEquatable<IndexedItem>
    {
        public IndexedItem(int itemId, byte fieldId)
        {
            this.ItemId = itemId;
            this.FieldId = fieldId;
        }

        public int ItemId { get; }
        public byte FieldId { get; }

        public override bool Equals(object obj)
        {
            return obj is IndexedItem item &&
                   this.Equals(item);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.ItemId, this.FieldId);
        }

        public bool Equals(IndexedItem other)
        {
            return this.ItemId == other.ItemId &&
                   this.FieldId == other.FieldId;
        }

        public static bool operator ==(IndexedItem left, IndexedItem right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IndexedItem left, IndexedItem right)
        {
            return !(left == right);
        }
    }
}
