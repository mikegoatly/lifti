using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// The result of a positional query intersection. This keeps track of all the locations
    /// that matched positionally, giving quick access to the max and min locations for reference
    /// in further positional intersections if needed.
    /// </summary>
    public struct CompositeTokenMatchLocation : ITokenLocationMatch, IEquatable<CompositeTokenMatchLocation>
    {
        private readonly ITokenLocationMatch leftToken;
        private readonly ITokenLocationMatch rightToken;
        private readonly Lazy<int> minTokenIndex;
        private readonly Lazy<int> maxTokenIndex;

        public CompositeTokenMatchLocation(ITokenLocationMatch leftToken, ITokenLocationMatch rightToken)
        {
            this.leftToken = leftToken;
            this.rightToken = rightToken;
            this.minTokenIndex = new Lazy<int>(() => Math.Min(leftToken.MinTokenIndex, rightToken.MinTokenIndex));
            this.maxTokenIndex = new Lazy<int>(() => Math.Max(leftToken.MaxTokenIndex, rightToken.MaxTokenIndex));
        }

        public int MaxTokenIndex => this.maxTokenIndex.Value;

        public int MinTokenIndex => this.minTokenIndex.Value;

        public override bool Equals(object obj)
        {
            return obj is CompositeTokenMatchLocation location &&
                   this.Equals(location);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.leftToken, this.rightToken);
        }

        public IEnumerable<TokenLocation> GetLocations()
        {
            return this.leftToken.GetLocations().Concat(this.rightToken.GetLocations());
        }

        public bool Equals(CompositeTokenMatchLocation other)
        {
            return this.leftToken.Equals(other.leftToken) &&
                   this.rightToken.Equals(other.rightToken);
        }

        public static bool operator ==(CompositeTokenMatchLocation left, CompositeTokenMatchLocation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CompositeTokenMatchLocation left, CompositeTokenMatchLocation right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return "Composite location - min:" + this.MinTokenIndex.ToString(CultureInfo.InvariantCulture) +
                " max: " + this.MaxTokenIndex.ToString(CultureInfo.InvariantCulture);
        }
    }
}