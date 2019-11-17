namespace Lifti
{
    internal class IndexRemovalMutation
    {
        private readonly IndexNode root;
        private readonly IIndexNodeFactory indexNodeFactory;

        public IndexRemovalMutation(IndexNode root, IIndexNodeFactory indexNodeFactory)
        {
            this.root = root;
            this.indexNodeFactory = indexNodeFactory;
        }

        internal IndexNode Remove(int itemId)
        {
            if (this.TryRemove(this.root, itemId, out var mutatedNode))
            {
                return mutatedNode;
            }

            // No mutations occurred
            return this.root;
        }

        private bool TryRemove(IndexNode node, int itemId, out IndexNode mutatedNode)
        {
            var mutated = false;
            var mutatedChildNodes = node.ChildNodes;

            if (node.HasChildNodes)
            {
                foreach (var child in node.ChildNodes)
                {
                    if (this.TryRemove(child.Value, itemId, out var mutatedChild))
                    {
                        mutated = true;
                        mutatedChildNodes = mutatedChildNodes.SetItem(child.Key, mutatedChild);
                    }
                }
            }

            var mutatedMatches = node.Matches;
            if (node.HasMatches)
            {
                mutatedMatches = node.Matches.Remove(itemId);
                if (mutatedMatches != node.Matches)
                {
                    mutated = true;
                }
            }

            mutatedNode = mutated ? this.indexNodeFactory.CreateNode(node.IntraNodeText, mutatedChildNodes, mutatedMatches) : node;
            return mutated;
        }
    }
}
