using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Lifti
{
    internal sealed class ChildNodeMapMutation
    {
        private readonly ChildNodeMap? original;
        private readonly Dictionary<char, IndexNodeMutation> mutated;
        private int newChildNodeCount;

        public ChildNodeMapMutation(char splitChar, IndexNodeMutation splitChildNode)
        {
            this.mutated = new()
            {
                { splitChar, splitChildNode }
            };

            this.newChildNodeCount = 1;
        }

        internal ChildNodeMapMutation(ChildNodeMap original)
        {
            this.original = original;
            this.mutated = [];
        }

        public IEnumerable<(char childCharacter, IndexNodeMutation childNode)> GetMutated()
        {
            foreach (var child in this.mutated)
            {
                yield return (child.Key, child.Value);
            }
        }

        public IEnumerable<(char childCharacter, IndexNode childNode)> GetUnmutated()
        {
            if (this.original is { } originalChildNodeMap)
            {
                foreach (var (childCharacter, childNode) in originalChildNodeMap.CharacterMap)
                {
                    if (!this.mutated.ContainsKey(childCharacter))
                    {
                        yield return (childCharacter, childNode);
                    }
                }
            }
        }

        internal ChildNodeMap Apply()
        {
            // Combine the original and mutated children.
            // We need to ensure:
            // 1. mutated children in the original list are replaced with the mutated version
            // 2. mutated children not in the original list are added to the list
            // 3. the resulting list is sorted in ascending order
            ChildNodeMapEntry[] newChildNodes;

            // TODO - this could be parallelised now we're setting elements into a fixed array (using Interlocked.Increment for i)
            var i = 0;
            if (this.original is { } originalChildNodeMap)
            {
                newChildNodes = new ChildNodeMapEntry[this.newChildNodeCount + originalChildNodeMap.Count];

                foreach (var (childChar, childNode) in originalChildNodeMap.CharacterMap)
                {
                    if (this.mutated.ContainsKey(childChar) == false)
                    {
                        // This child node is not mutated, so add it to the list
                        newChildNodes[i++] = new(childChar, childNode);
                    }
                }
            }
            else
            {
                Debug.Assert(this.newChildNodeCount == this.mutated.Count);
                newChildNodes = new ChildNodeMapEntry[this.mutated.Count];
            }

            // Add the mutated children to the list
            foreach (var mutation in this.mutated)
            {
                Debug.Assert(i < newChildNodes.Length);
                newChildNodes[i++] = new(mutation.Key, mutation.Value.Apply());
            }

            Debug.Assert(i == newChildNodes.Length, "Expected all elements to have been populated");

            // Sort the list in-place
            Array.Sort(newChildNodes, (x, y) => x.ChildChar.CompareTo(y.ChildChar));

            return new ChildNodeMap(newChildNodes);
        }

        internal IndexNodeMutation GetOrCreateMutation(char indexChar, Func<IndexNodeMutation> createMutatedNode)
        {
            if (!this.mutated.TryGetValue(indexChar, out var mutation))
            {
                mutation = createMutatedNode();
                this.Mutate(indexChar, mutation);

                if (this.original?.TryGetValue(indexChar, out var _) != true)
                {
                    this.newChildNodeCount++;
                }
            }

            return mutation;
        }

        internal void Mutate(char childChar, IndexNodeMutation mutatedChild)
        {
            this.mutated[childChar] = mutatedChild;
        }

        internal void ToString(StringBuilder builder, int depth)
        {
            foreach (var (character, childNode) in this.GetUnmutated())
            {
                builder.AppendLine();
                childNode.ToString(builder, character, depth);
            }

            foreach (var (character, childNode) in this.GetMutated())
            {
                builder.AppendLine();
                childNode.ToString(builder, character, depth);
            }
        }
    }

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

        internal ChildNodeMap(ChildNodeMapEntry[] map)
        {
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
        internal IReadOnlyList<ChildNodeMapEntry> CharacterMap => this.childNodes;

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

                    var index = Array.BinarySearch(this.childNodes, value, ChildCharComparer.Instance);
                    if (index < 0)
                    {
                        nextNode = null;
                        return false;
                    }

                    nextNode = this.childNodes[index].ChildNode;
                    return true;
            }
        }

        private class ChildCharComparer : System.Collections.IComparer
        {
            public static ChildCharComparer Instance { get; } = new ChildCharComparer();

            public int Compare(object? x, object? y)
            {
                if (x is ChildNodeMapEntry entry && y is char character)
                {
                    return entry.ChildChar.CompareTo(character);
                }

                throw new ArgumentException("Cannot compare the specified objects");
            }
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
