using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Lifti
{
    /// <summary>
    /// An entry in <see cref="ChildNodeMap"/>.
    /// </summary>
    public record struct ChildNodeMapEntry(char ChildChar, IndexNode ChildNode);

    /// <summary>
    /// An immutable map of child nodes.
    /// </summary>
    public readonly struct ChildNodeMap : IEquatable<ChildNodeMap>
    {
        private readonly ChildNodeMapEntry[] childNodes;

        /// <summary>
        /// Initializes a new empty instance of <see cref="ChildNodeMap"/>
        /// </summary>
        public ChildNodeMap()
        {
            this.childNodes = [];
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ChildNodeMap"/>.
        /// </summary>
        /// <param name="map">
        /// The child nodes to initialize the map with.
        /// </param>
        public ChildNodeMap(ChildNodeMapEntry[] map)
        {
            ArgumentNullException.ThrowIfNull(map);

            // Verify that the map is sorted
#if DEBUG
            for (var i = 1; i < map.Length; i++)
            {
                Debug.Assert(map[i - 1].ChildChar < map[i].ChildChar);
            }
#endif

            this.childNodes = map;
        }

        /// <summary>
        /// Gets an empty instance of <see cref="ChildNodeMap"/>.
        /// </summary>
        public static ChildNodeMap Empty { get; } = new ChildNodeMap();

        /// <summary>
        /// Gets the number of child nodes in the map.
        /// </summary>
        public int Count => this.childNodes.Length;

        /// <summary>
        /// Gets the set of characters that link from this instance to the child nodes.
        /// </summary>
        public IReadOnlyList<ChildNodeMapEntry> CharacterMap => this.childNodes;

        internal ChildNodeMapMutation StartMutation()
        {
            return new ChildNodeMapMutation(this);
        }

        /// <summary>
        /// Tries to get the child node for the specified character.
        /// </summary>
        public bool TryGetValue(char value, [NotNullWhen(true)] out IndexNode? nextNode)
        {
            char character;
            var length = this.childNodes.Length;
            switch (length)
            {
                case 0:
                    nextNode = null;
                    return false;

                case 1:
                    (character, nextNode) = this.childNodes[0];
                    if (character == value)
                    {
                        return true;
                    }

                    return false;

                case 2:
                    (character, nextNode) = this.childNodes[0];
                    if (character == value)
                    {
                        return true;
                    }

                    (character, nextNode) = this.childNodes[1];
                    if (character == value)
                    {
                        return true;
                    }

                    return false;

                default:
                    // General case - check bounds, then do a binary search if we're in range
                    if (value < this.childNodes[0].ChildChar || value > this.childNodes[length - 1].ChildChar)
                    {
                        nextNode = null;
                        return false;
                    }

                    nextNode = BinarySearchChildNodes(value);
                    return nextNode is not null;
            }
        }

        private IndexNode? BinarySearchChildNodes(char value)
        {
            // We don't want to use Array.BinarySearch here because of the need to use a custom comparer.
            // This custom implementation is significantly faster because we don't get involved in 
            // any boxing/unboxing of the value types.
            var left = 0;
            var right = this.childNodes.Length - 1;

            while (left <= right)
            {
                var middle = left + (right - left) / 2;
                var middleChar = this.childNodes[middle].ChildChar;

                if (middleChar == value)
                {
                    return this.childNodes[middle].ChildNode;
                }

                if (middleChar < value)
                {
                    left = middle + 1;
                }
                else
                {
                    right = middle - 1;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            // Because we're immutable, we can use reference equality
            return obj is ChildNodeMap other
                && this.Equals(other);
        }

        /// <inheritdoc />
        public bool Equals(ChildNodeMap other)
        {
            return other.childNodes == this.childNodes;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.childNodes);
        }

        /// <inheritdoc />
        public static bool operator ==(ChildNodeMap left, ChildNodeMap right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc />
        public static bool operator !=(ChildNodeMap left, ChildNodeMap right)
        {
            return !(left == right);
        }
    }
}
