using System;
using System.Collections.Generic;
using System.Diagnostics;
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
}
