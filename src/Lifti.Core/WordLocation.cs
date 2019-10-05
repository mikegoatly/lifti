using System;

namespace Lifti
{
    public struct WordLocation : IComparable<WordLocation>, IEquatable<WordLocation>
    {
        public WordLocation(int wordIndex, int start, int length)
        {
            this.WordIndex = wordIndex;
            this.Start = start;
            this.Length = length;
        }

        public int WordIndex { get; }
        public int Start { get; }
        public int Length { get; }

        public override bool Equals(object obj)
        {
            return obj is WordLocation location &&
                   ((IEquatable<WordLocation>)this).Equals(location);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Start, this.Length, this.WordIndex);
        }

        public override string ToString()
        {
            return $"#{this.WordIndex} [{this.Start},{this.Length}]";
        }

        public int CompareTo(WordLocation other)
        {
            var result = this.Start.CompareTo(other.Start);
            if (result == 0)
            {
                result = this.Length.CompareTo(other.Length);
            }

            if (result == 0)
            {
                result = this.WordIndex.CompareTo(other.WordIndex);
            }

            return result;
        }

        bool IEquatable<WordLocation>.Equals(WordLocation location)
        {
            return this.Start == location.Start &&
                   this.Length == location.Length &&
                   this.WordIndex == location.WordIndex;
        }

        public static bool operator ==(WordLocation left, WordLocation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(WordLocation left, WordLocation right)
        {
            return !(left == right);
        }

        public static bool operator <(WordLocation left, WordLocation right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(WordLocation left, WordLocation right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(WordLocation left, WordLocation right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(WordLocation left, WordLocation right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}
