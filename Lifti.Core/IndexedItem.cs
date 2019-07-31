using System;

namespace Lifti
{
    public struct IndexedItem
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
                   this.ItemId == item.ItemId &&
                   this.FieldId == item.FieldId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.ItemId, this.FieldId);
        }
    }
}
