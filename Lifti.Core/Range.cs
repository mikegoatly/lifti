using System;

namespace Lifti
{
    public struct Range : IComparable<Range>, IEquatable<Range>
    {
        public Range(int start, int length)
        {
            this.Start = start;
            this.Length = length;
        }

        public int Start { get; }
        public int Length { get; }

        public override bool Equals(object obj)
        {
            return obj is Range range &&
                   ((IEquatable<Range>)this).Equals(range);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Start, this.Length);
        }

        public override string ToString()
        {
            return $"[{this.Start},{this.Length}]";
        }

        int IComparable<Range>.CompareTo(Range other)
        {
            var result = this.Start.CompareTo(other.Start);
            if (result == 0)
            {
                result = this.Length.CompareTo(other.Length);
            }

            return result;
        }

        bool IEquatable<Range>.Equals(Range range)
        {
            return this.Start == range.Start &&
                   this.Length == range.Length;
        }
    }
}
