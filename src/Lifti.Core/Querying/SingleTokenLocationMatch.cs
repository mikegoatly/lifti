using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lifti.Querying
{
    /// <summary>
    /// Represents the location of a single token manifested during the execution of a query.
    /// </summary>
    public readonly struct SingleTokenLocationMatch : ITokenLocationMatch, IEquatable<SingleTokenLocationMatch>
    {
        private readonly TokenLocation original;

        /// <summary>
        /// Constructs a new instance of <see cref="SingleTokenLocationMatch"/>.
        /// </summary>
        public SingleTokenLocationMatch(TokenLocation original)
        {
            this.original = original;
        }

        /// <inheritdoc />
        public int MaxTokenIndex => this.original.TokenIndex;

        /// <inheritdoc />
        public int MinTokenIndex => this.original.TokenIndex;

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is SingleTokenLocationMatch match &&
                   this.Equals(match);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.original);
        }

        /// <inheritdoc />
        public IEnumerable<TokenLocation> GetLocations()
        {
            return new[] { original };
        }

        /// <inheritdoc />
        public bool Equals(SingleTokenLocationMatch other)
        {
            return this.original.Equals(other.original);
        }

        /// <inheritdoc />
        public static bool operator ==(SingleTokenLocationMatch left, SingleTokenLocationMatch right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc />
        public static bool operator !=(SingleTokenLocationMatch left, SingleTokenLocationMatch right)
        {
            return !(left == right);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "Single index: " + this.original.TokenIndex.ToString(CultureInfo.InvariantCulture);
        }
    }
}
