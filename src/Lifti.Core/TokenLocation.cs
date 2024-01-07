using Lifti.Querying;
using System;
using System.Collections.Generic;

namespace Lifti
{
    /// <summary>
    /// Provides information about the location of a token in the original text.
    /// </summary>
    public class TokenLocation : IComparable<TokenLocation>, IEquatable<TokenLocation>, ITokenLocation
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

        int ITokenLocation.MaxTokenIndex => this.TokenIndex;

        int ITokenLocation.MinTokenIndex => this.TokenIndex;

        void ITokenLocation.AddTo(HashSet<TokenLocation> collector)
        {
            collector.Add(this);
        }

        CompositeTokenLocation ITokenLocation.ComposeWith(ITokenLocation other)
        {
            return other switch
            {
                CompositeTokenLocation composite => composite.ComposeWith(this),
                TokenLocation location => new CompositeTokenLocation(
                    [this, location],
                    Math.Min(this.TokenIndex, location.TokenIndex),
                    Math.Max(this.TokenIndex, location.TokenIndex)),
                _ => throw new InvalidOperationException($"Cannot compose a {nameof(TokenLocation)} with a {other.GetType().Name}"),
            };
        }

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
        public virtual int CompareTo(TokenLocation? other)
        {
            if (other is null)
            {
                return 1;
            }

            var result = this.TokenIndex.CompareTo(other.TokenIndex);
            if (result == 0)
            {
                result = this.Start.CompareTo(other.Start);
            }

            if (result == 0)
            {
                result = this.Length.CompareTo(other.Length);
            }

            return result;
        }

        /// <inheritdoc/>
        public virtual bool Equals(TokenLocation? other)
        {
            return other is not null &&
                 this.TokenIndex == other.TokenIndex &&
                 this.Start == other.Start &&
                 this.Length == other.Length;
        }

        int IComparable<ITokenLocation>.CompareTo(ITokenLocation? other)
        {
            if (other is null)
            {
                return -1;
            }

            if (other is TokenLocation location)
            {
                return this.TokenIndex.CompareTo(location.TokenIndex);
            }

            var result = this.TokenIndex.CompareTo(other.MinTokenIndex);

            if (result == 0)
            {
                // When comparing a single token location to a composite location, we'll
                // always treat the single token location as being less than the composite
                // location.
                return -1;
            }

            return result;
        }

        bool IEquatable<ITokenLocation>.Equals(ITokenLocation? other)
        {
            if (other is TokenLocation location)
            {
                return this.Equals(location);
            }

            return false;
        }

        /// <inheritdoc/>
        public static bool operator ==(TokenLocation? left, TokenLocation? right)
        {
            return left?.Equals(right) ?? false;
        }

        /// <inheritdoc/>
        public static bool operator !=(TokenLocation? left, TokenLocation? right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public static bool operator <(TokenLocation? left, TokenLocation? right)
        {
            return (left?.CompareTo(right) ?? -1) < 0;
        }

        /// <inheritdoc/>
        public static bool operator <=(TokenLocation? left, TokenLocation? right)
        {
            return (left?.CompareTo(right) ?? -1) <= 0;
        }

        /// <inheritdoc/>
        public static bool operator >(TokenLocation? left, TokenLocation? right)
        {
            return (left?.CompareTo(right) ?? -1) > 0;
        }

        /// <inheritdoc/>
        public static bool operator >=(TokenLocation? left, TokenLocation? right)
        {
            return (left?.CompareTo(right) ?? -1) >= 0;
        }
    }
}
