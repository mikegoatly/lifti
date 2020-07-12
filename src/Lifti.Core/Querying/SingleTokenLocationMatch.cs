using System;
using System.Collections.Generic;

namespace Lifti.Querying
{
    public struct SingleTokenLocationMatch : ITokenLocationMatch, IEquatable<SingleTokenLocationMatch>
    {
        private readonly TokenLocation original;

        public int MaxTokenIndex => this.original.TokenIndex;

        public int MinTokenIndex => this.original.TokenIndex;

        public SingleTokenLocationMatch(TokenLocation original)
        {
            this.original = original;
        }

        public override bool Equals(object obj)
        {
            return obj is SingleTokenLocationMatch match &&
                   this.Equals(match);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.original);
        }

        public IEnumerable<TokenLocation> GetLocations()
        {
            return new[] { original };
        }

        public bool Equals(SingleTokenLocationMatch other)
        {
            return this.original.Equals(other.original);
        }

        public static bool operator ==(SingleTokenLocationMatch left, SingleTokenLocationMatch right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SingleTokenLocationMatch left, SingleTokenLocationMatch right)
        {
            return !(left == right);
        }
    }
}
