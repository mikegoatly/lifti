using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Lifti
{
    internal sealed class ChildNodeMapMutation
    {
        private readonly ChildNodeMap? original;
        private readonly Dictionary<char, IndexNodeMutation> mutated;

        public ChildNodeMapMutation(char splitChar, IndexNodeMutation splitChildNode)
        {
            this.mutated = new()
            {
                { splitChar, splitChildNode }
            };
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
                foreach (var (childCharacter, childNode) in originalChildNodeMap.Enumerate())
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
            var newCount = this.mutated.Values.Where(x => x.IsNewNode).Count();
            List<(char childChar, IndexNode childNode)> newChildNodes;

            if (this.original is { } originalChildNodeMap)
            {
                newChildNodes = new(newCount + originalChildNodeMap.Count);
                foreach (var (childChar, childNode) in originalChildNodeMap.Enumerate())
                {
                    if (this.mutated.ContainsKey(childChar) == false)
                    {
                        // This child node is not mutated, so add it to the list
                        newChildNodes.Add((childChar, childNode));
                    }
                }
            }
            else
            {
                newChildNodes = new(newCount);
            }

            // Add the mutated children to the list
            foreach (var mutation in this.mutated)
            {
                newChildNodes.Add((mutation.Key, mutation.Value.Apply()));
            }

            // Sort the list in-place
            newChildNodes.Sort((x, y) => x.childChar.CompareTo(y.childChar));

            return new ChildNodeMap(newChildNodes);
        }

        internal IndexNodeMutation GetOrCreateMutation(char indexChar, Func<IndexNodeMutation> createMutatedNode)
        {
            if (!this.mutated.TryGetValue(indexChar, out var mutation))
            {
                mutation = createMutatedNode();
                this.Mutate(indexChar, mutation);
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
    /// An immutable map of child nodes.
    /// </summary>
    public readonly struct ChildNodeMap : IEquatable<ChildNodeMap>
    {
        private readonly char[] childChars;
        private readonly IndexNode[] childNodes;

        internal ChildNodeMap(List<(char childChar, IndexNode childNode)> map)
        {
            // Verify that the map is sorted
#if DEBUG
            for (var i = 1; i < map.Count; i++)
            {
                Debug.Assert(map[i - 1].childChar < map[i].childChar);
            }
#endif

            this.childChars = new char[map.Count];
            this.childNodes = new IndexNode[map.Count];

            for (var i = 0; i < map.Count; i++)
            {
                this.childChars[i] = map[i].childChar;
                this.childNodes[i] = map[i].childNode;
            }
        }

        private ChildNodeMap(char[] childChars, IndexNode[] childNodes)
        {
            this.childNodes = childNodes;
            this.childChars = childChars;
        }

        /// <summary>
        /// Gets an empty instance of <see cref="ChildNodeMap"/>.
        /// </summary>
        public static ChildNodeMap Empty { get; } = new ChildNodeMap([], []);

        /// <summary>
        /// Gets the number of child nodes in the map.
        /// </summary>
        public int Count => this.childChars.Length;

        /// <summary>
        /// Gets the set of characters that link from this instance to the child nodes.
        /// </summary>
        public ReadOnlyMemory<char> Characters => this.childChars;

        internal ChildNodeMapMutation StartMutation()
        {
            return new ChildNodeMapMutation(this);
        }

        /// <summary>
        /// Enumerates all the child nodes in the map.
        /// </summary>
        public IEnumerable<(char character, IndexNode childNode)> Enumerate()
        {
            for (var i = 0; i < this.childChars.Length; i++)
            {
                yield return (this.childChars[i], this.childNodes[i]);
            }
        }

        /// <summary>
        /// Tries to get the child node for the specified character.
        /// </summary>
        public bool TryGetValue(char value, [NotNullWhen(true)] out IndexNode? nextNode)
        {
            if (this.childChars.Length == 0)
            {
                nextNode = null;
                return false;
            }

            // TODO: Is this faster if we check for the value being outside the range of the array first?
            var index = Array.BinarySearch(this.childChars, value);
            if (index < 0)
            {
                nextNode = null;
                return false;
            }

            nextNode = this.childNodes[index];
            return true;
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
            return other.childNodes == this.childNodes
                && other.childChars == this.childChars;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.childChars, this.childNodes);
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
