using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    public struct IndexedWord : IEquatable<IndexedWord>
    {
        public IndexedWord(byte fieldId, params WordLocation[] locations)
        {
            this.FieldId = fieldId;
            this.Locations = locations;
        }

        public IndexedWord(byte fieldId, IReadOnlyList<WordLocation> locations)
        {
            this.FieldId = fieldId;
            this.Locations = locations;
        }

        public byte FieldId { get; }
        public IReadOnlyList<WordLocation> Locations { get; }

        public override bool Equals(object obj)
        {
            return obj is IndexedWord location &&
                this.Equals(location);
        }

        public override int GetHashCode()
        {
            var hashCode = HashCode.Combine(this.FieldId);
            foreach (var location in this.Locations)
            {
                hashCode = HashCode.Combine(hashCode, location);
            }

            return hashCode;
        }

        public bool Equals(IndexedWord other)
        {
            return this.FieldId == other.FieldId &&
                   this.Locations.SequenceEqual(other.Locations);
        }

        public static bool operator ==(IndexedWord left, IndexedWord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IndexedWord left, IndexedWord right)
        {
            return !(left == right);
        }
    }
}
