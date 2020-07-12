using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    public struct IndexedToken : IEquatable<IndexedToken>
    {
        public IndexedToken(byte fieldId, params TokenLocation[] locations)
        {
            this.FieldId = fieldId;
            this.Locations = locations;
        }

        public IndexedToken(byte fieldId, IReadOnlyList<TokenLocation> locations)
        {
            this.FieldId = fieldId;
            this.Locations = locations;
        }

        public byte FieldId { get; }
        public IReadOnlyList<TokenLocation> Locations { get; }

        public override bool Equals(object obj)
        {
            return obj is IndexedToken location &&
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

        public bool Equals(IndexedToken other)
        {
            return this.FieldId == other.FieldId &&
                   this.Locations.SequenceEqual(other.Locations);
        }

        public static bool operator ==(IndexedToken left, IndexedToken right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IndexedToken left, IndexedToken right)
        {
            return !(left == right);
        }
    }
}
