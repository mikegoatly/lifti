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
    internal sealed class CompositeTokenLocation : ITokenLocation
    {
        private readonly TokenLocation[] locations;

        /// <summary>
        /// Constructs a new instance of <see cref="CompositeTokenLocation"/>.
        /// </summary>
        internal CompositeTokenLocation(TokenLocation[] locations, int minTokenIndex, int maxTokenIndex)
        {
            this.locations = locations;
            this.MinTokenIndex = minTokenIndex;
            this.MaxTokenIndex = maxTokenIndex;
        }

        public int MaxTokenIndex { get; }

        public int MinTokenIndex { get; }

        public void AddTo(HashSet<TokenLocation> collector)
        {
            for (var i = 0; i < this.locations.Length; i++)
            {
                collector.Add(this.locations[i]);
            }
        }

        public CompositeTokenLocation ComposeWith(ITokenLocation other)
        {
            var currentLength = this.locations.Length;
            switch (other)
            {
                case CompositeTokenLocation composite:
                    // We need to build a new array capable of storing both sets of locations
                    var additionLength = composite.locations.Length;
                    var newLocations = new TokenLocation[currentLength + additionLength];
                    Array.Copy(this.locations, newLocations, currentLength);
                    Array.Copy(composite.locations, 0, newLocations, currentLength, additionLength);

                    return new CompositeTokenLocation(
                        newLocations,
                        Math.Min(this.MinTokenIndex, composite.MinTokenIndex),
                        Math.Max(this.MaxTokenIndex, composite.MaxTokenIndex));


                case TokenLocation location:
                    // Just one more element to add
                    newLocations = new TokenLocation[currentLength + 1];
                    Array.Copy(this.locations, newLocations, currentLength);
                    newLocations[currentLength] = location;

                    var newTokenIndex = location.TokenIndex;
                    return new CompositeTokenLocation(
                        newLocations,
                        Math.Min(this.MinTokenIndex, newTokenIndex),
                        Math.Max(this.MaxTokenIndex, newTokenIndex));

                default:
                    throw new InvalidOperationException($"Cannot compose a {nameof(TokenLocation)} with a {other.GetType().Name}");
            }
        }

        /// <inheritdoc/>
        public bool Equals(ITokenLocation? other)
        {
            return other switch
            {
                CompositeTokenLocation composite => this.locations.SequenceEqual(composite.locations),
                _ => false,
            };
        }

        /// <inheritdoc/>
        public int CompareTo(ITokenLocation? other)
        {
            if (other is { } ITokenLocation)
            {
                var result = this.MinTokenIndex.CompareTo(other.MinTokenIndex);
                if (result == 0)
                {
                    result = this.MaxTokenIndex.CompareTo(other.MaxTokenIndex);
                }

                return result;
            }

            return -1;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Composite location - min:" + this.MinTokenIndex.ToString(CultureInfo.InvariantCulture) +
                " max: " + this.MaxTokenIndex.ToString(CultureInfo.InvariantCulture);
        }
    }
}