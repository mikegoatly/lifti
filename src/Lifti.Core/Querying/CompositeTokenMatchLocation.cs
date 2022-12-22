using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// Represents the result of a positional query intersection. This keeps track of all the locations
    /// that matched positionally, giving quick access to the max and min locations for reference
    /// in further positional intersections if needed.
    /// </summary>
    public readonly struct CompositeTokenMatchLocation : ITokenLocationMatch, IEquatable<CompositeTokenMatchLocation>
    {
        private readonly ITokenLocationMatch leftToken;
        private readonly ITokenLocationMatch rightToken;
        private readonly Lazy<int> minTokenIndex;
        private readonly Lazy<int> maxTokenIndex;

        /// <summary>
        /// Constructs a new instance of <see cref="CompositeTokenMatchLocation"/>.
        /// </summary>
        public CompositeTokenMatchLocation(ITokenLocationMatch leftToken, ITokenLocationMatch rightToken)
        {
            this.leftToken = leftToken;
            this.rightToken = rightToken;
            this.minTokenIndex = new Lazy<int>(() => Math.Min(leftToken.MinTokenIndex, rightToken.MinTokenIndex));
            this.maxTokenIndex = new Lazy<int>(() => Math.Max(leftToken.MaxTokenIndex, rightToken.MaxTokenIndex));
        }

        /// <inheritdoc/>
        public int MaxTokenIndex => this.maxTokenIndex.Value;

        /// <inheritdoc/>
        public int MinTokenIndex => this.minTokenIndex.Value;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is CompositeTokenMatchLocation location &&
                   this.Equals(location);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(this.leftToken, this.rightToken);
        }

        /// <inheritdoc/>
        public IEnumerable<TokenLocation> GetLocations()
        {
            return this.leftToken.GetLocations().Concat(this.rightToken.GetLocations());
        }

        /// <inheritdoc/>
        public bool Equals(CompositeTokenMatchLocation other)
        {
            return this.leftToken.Equals(other.leftToken) &&
                   this.rightToken.Equals(other.rightToken);
        }

        /// <inheritdoc/>
        public static bool operator ==(CompositeTokenMatchLocation left, CompositeTokenMatchLocation right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(CompositeTokenMatchLocation left, CompositeTokenMatchLocation right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Composite location - min:" + this.MinTokenIndex.ToString(CultureInfo.InvariantCulture) +
                " max: " + this.MaxTokenIndex.ToString(CultureInfo.InvariantCulture);
        }
    }
}