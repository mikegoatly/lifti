using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public struct FieldMatch : IEquatable<FieldMatch>
    {
        public FieldMatch(IndexedWord word)
            : this(word.FieldId, word.Locations)
        {
        }

        public FieldMatch(byte fieldId, IReadOnlyList<WordLocation> locations)
        {
            this.FieldId = fieldId;
            this.Locations = locations.Select(l => (IWordLocationMatch)new SingleWordLocationMatch(l)).ToList();
        }

        public FieldMatch(byte fieldId, params IWordLocationMatch[] locations)
        {
            this.FieldId = fieldId;
            this.Locations = locations;
        }

        public byte FieldId { get; }

        public IReadOnlyList<IWordLocationMatch> Locations { get; }

        public override bool Equals(object obj)
        {
            return obj is FieldMatch match &&
                this.Equals(match);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.FieldId, this.Locations);
        }

        public bool Equals(FieldMatch other)
        {
            return this.FieldId == other.FieldId &&
                   this.Locations.SequenceEqual(other.Locations);
        }

        public static bool operator ==(FieldMatch left, FieldMatch right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FieldMatch left, FieldMatch right)
        {
            return !(left == right);
        }

        public IReadOnlyList<WordLocation> GetWordLocations()
        {
            return this.Locations.SelectMany(l => l.GetLocations()).ToList();
        }
    }
}
