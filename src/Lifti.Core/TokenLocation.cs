using System;

namespace Lifti
{
    public struct TokenLocation : IComparable<TokenLocation>, IEquatable<TokenLocation>
    {
        public TokenLocation(int tokenIndex, int start, ushort length)
        {
            this.TokenIndex = tokenIndex;
            this.Start = start;
            this.Length = length;
        }

        public int TokenIndex { get; }
        public int Start { get; }
        public ushort Length { get; }

        public override bool Equals(object obj)
        {
            return obj is TokenLocation location &&
                   ((IEquatable<TokenLocation>)this).Equals(location);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Start, this.Length, this.TokenIndex);
        }

        public override string ToString()
        {
            return $"#{this.TokenIndex} [{this.Start},{this.Length}]";
        }

        public int CompareTo(TokenLocation other)
        {
            var result = this.Start.CompareTo(other.Start);
            if (result == 0)
            {
                result = this.Length.CompareTo(other.Length);
            }

            if (result == 0)
            {
                result = this.TokenIndex.CompareTo(other.TokenIndex);
            }

            return result;
        }

        bool IEquatable<TokenLocation>.Equals(TokenLocation location)
        {
            return this.Start == location.Start &&
                   this.Length == location.Length &&
                   this.TokenIndex == location.TokenIndex;
        }

        public static bool operator ==(TokenLocation left, TokenLocation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TokenLocation left, TokenLocation right)
        {
            return !(left == right);
        }

        public static bool operator <(TokenLocation left, TokenLocation right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(TokenLocation left, TokenLocation right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(TokenLocation left, TokenLocation right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(TokenLocation left, TokenLocation right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}
