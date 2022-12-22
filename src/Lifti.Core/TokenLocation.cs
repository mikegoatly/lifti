using System;

namespace Lifti
{
    /// <summary>
    /// Provides information about the location of a token in the original text.
    /// </summary>
    public readonly struct TokenLocation : IComparable<TokenLocation>, IEquatable<TokenLocation>
    {
        /// <summary>
        /// Constructs a new <see cref="TokenLocation"/> instance.
        /// </summary>
        public TokenLocation(int tokenIndex, int start, ushort length)
        {
            this.TokenIndex = tokenIndex;
            this.Start = start;
            this.Length = length;
        }

        /// <summary>
        /// Gets the index of the token in the document.
        /// </summary>
        public int TokenIndex { get; }

        /// <summary>
        /// Gets the start offset of the token in the document's text.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Gets the length of the token.
        /// </summary>
        public ushort Length { get; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is TokenLocation location &&
                   ((IEquatable<TokenLocation>)this).Equals(location);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Start, this.Length, this.TokenIndex);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"#{this.TokenIndex} [{this.Start},{this.Length}]";
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        bool IEquatable<TokenLocation>.Equals(TokenLocation location)
        {
            return this.Start == location.Start &&
                   this.Length == location.Length &&
                   this.TokenIndex == location.TokenIndex;
        }

        /// <inheritdoc/>
        public static bool operator ==(TokenLocation left, TokenLocation right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(TokenLocation left, TokenLocation right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public static bool operator <(TokenLocation left, TokenLocation right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <inheritdoc/>
        public static bool operator <=(TokenLocation left, TokenLocation right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <inheritdoc/>
        public static bool operator >(TokenLocation left, TokenLocation right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <inheritdoc/>
        public static bool operator >=(TokenLocation left, TokenLocation right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}
