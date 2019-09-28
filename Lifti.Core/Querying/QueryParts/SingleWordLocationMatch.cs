using System;
using System.Collections.Generic;

namespace Lifti.Querying.QueryParts
{
    public struct SingleWordLocationMatch : IWordLocationMatch, IEquatable<SingleWordLocationMatch>
    {
        private readonly WordLocation original;

        public SingleWordLocationMatch(WordLocation original)
        {
            this.original = original;
        }

        public override bool Equals(object obj)
        {
            return obj is SingleWordLocationMatch match &&
                   this.Equals(match);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.original);
        }

        public IEnumerable<WordLocation> GetLocations()
        {
            return new[] { original };
        }

        public bool Equals(SingleWordLocationMatch other)
        {
            return this.original.Equals(other.original);
        }

        public static bool operator ==(SingleWordLocationMatch left, SingleWordLocationMatch right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SingleWordLocationMatch left, SingleWordLocationMatch right)
        {
            return !(left == right);
        }
    }
}
