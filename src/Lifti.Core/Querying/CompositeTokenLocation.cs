using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lifti.Querying
{
    /// <summary>
    /// Represents the result of a positional query intersection. This keeps track of all the locations
    /// that matched positionally, giving quick access to the max and min locations for reference
    /// in further positional intersections if needed.
    /// </summary>
    internal sealed class CompositeTokenLocation : ITokenLocation
    {
        private readonly ITokenLocation leftToken;
        private readonly ITokenLocation rightToken;
        private readonly Lazy<int> minTokenIndex;
        private readonly Lazy<int> maxTokenIndex;

        /// <summary>
        /// Constructs a new instance of <see cref="CompositeTokenLocation"/>.
        /// </summary>
        public CompositeTokenLocation(ITokenLocation leftToken, ITokenLocation rightToken)
        {
            this.leftToken = leftToken;
            this.rightToken = rightToken;
            this.minTokenIndex = new Lazy<int>(() => Math.Min(leftToken.MinTokenIndex, rightToken.MinTokenIndex));
            this.maxTokenIndex = new Lazy<int>(() => Math.Max(leftToken.MaxTokenIndex, rightToken.MaxTokenIndex));
        }

        public int MaxTokenIndex => this.maxTokenIndex.Value;

        public int MinTokenIndex => this.minTokenIndex.Value;

        public void AddTo(HashSet<TokenLocation> collector)
        {
            this.leftToken.AddTo(collector);
            this.rightToken.AddTo(collector);
        }

        /// <inheritdoc/>
        public bool Equals(ITokenLocation? other)
        {
            switch (other)
            {
                case CompositeTokenLocation composite:
                    return this.leftToken.Equals(composite.leftToken) &&
                        this.rightToken.Equals(composite.rightToken);
                default:
                    return false;
            }
        }

        /// <inheritdoc/>
        public int CompareTo(ITokenLocation? other)
        {
            switch (other)
            {
                case CompositeTokenLocation composite:
                    var leftCompare = this.leftToken.CompareTo(composite.leftToken);
                    if (leftCompare != 0)
                    {
                        return leftCompare;
                    }

                    return this.rightToken.CompareTo(composite.rightToken);
                default:
                    return -1;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Composite location - min:" + this.MinTokenIndex.ToString(CultureInfo.InvariantCulture) +
                " max: " + this.MaxTokenIndex.ToString(CultureInfo.InvariantCulture);
        }
    }
}