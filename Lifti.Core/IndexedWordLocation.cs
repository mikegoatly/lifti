using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    public struct IndexedWordLocation : IEquatable<IndexedWordLocation>
    {
        public IndexedWordLocation(byte fieldId, params Range[] locations)
        {
            this.FieldId = fieldId;
            this.Locations = locations;
        }

        public IndexedWordLocation(byte fieldId, IReadOnlyList<Range> locations)
        {
            this.FieldId = fieldId;
            this.Locations = locations;
        }

        public byte FieldId { get; }
        public IReadOnlyList<Range> Locations { get; }

        public override bool Equals(object obj)
        {
            return obj is IndexedWordLocation location &&
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

        public bool Equals(IndexedWordLocation other)
        {
            return this.FieldId == other.FieldId &&
                   this.Locations.SequenceEqual(other.Locations);
        }

        public static bool operator ==(IndexedWordLocation left, IndexedWordLocation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IndexedWordLocation left, IndexedWordLocation right)
        {
            return !(left == right);
        }
    }
}
