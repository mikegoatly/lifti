using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    public struct IndexedWordLocation
    {
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
                   this.FieldId == location.FieldId &&
                   this.Locations.SequenceEqual(location.Locations);
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
    }
}
