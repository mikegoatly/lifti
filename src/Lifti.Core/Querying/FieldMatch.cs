using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public struct FieldMatch : IEquatable<FieldMatch>
    {
        public FieldMatch(IndexedToken token)
            : this(token.FieldId, token.Locations)
        {
        }

        public FieldMatch(byte fieldId, IReadOnlyList<ITokenLocationMatch> locations)
        {
            this.FieldId = fieldId;
            this.Locations = locations;
        }

        public FieldMatch(byte fieldId, params ITokenLocationMatch[] locations)
        {
            this.FieldId = fieldId;
            this.Locations = locations;
        }

        private FieldMatch(byte fieldId, IReadOnlyList<TokenLocation> locations)
        {
            this.FieldId = fieldId;
            this.Locations = locations.Select(l => (ITokenLocationMatch)new SingleTokenLocationMatch(l)).ToList();
        }

        public byte FieldId { get; }

        public IReadOnlyList<ITokenLocationMatch> Locations { get; }

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

        public IReadOnlyList<TokenLocation> GetTokenLocations()
        {
            return this.Locations.SelectMany(l => l.GetLocations())
                .Distinct()
                .OrderBy(l => l.TokenIndex)
                .ToList();
        }
    }
}
