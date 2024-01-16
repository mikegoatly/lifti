using System.Collections.Generic;

#if NETSTANDARD
using System.Linq;
#endif

namespace Lifti
{
    internal sealed class DocumentTokenMatchMapMutation
    {
        private readonly DocumentTokenMatchMap original;
        private HashSet<int>? removed;
        private Dictionary<int, List<IndexedToken>>? mutated;

        public DocumentTokenMatchMapMutation(DocumentTokenMatchMap original)
        {
            this.original = original;
        }

        public DocumentTokenMatchMap Apply()
        {
            if (this.mutated == null && this.removed == null)
            {
                return this.original;
            }

            // Copy the original matches except any that have been expressly removed
            Dictionary<int, IReadOnlyList<IndexedToken>> mutatedMatches = new(this.original.DocumentTokenLookup);
            if (this.removed != null)
            {
                foreach (var documentId in this.removed)
                {
                    mutatedMatches.Remove(documentId);
                }
            }

            if (this.mutated != null)
            {
#if !NETSTANDARD
                // Avoid re-allocations by ensuring the dictionary has enough capacity to hold all the new items
                // In some situations this may actually be more than we need (A large number of documents have
                // been reindexed), but this is better in most cases. We could track the number of "new" documents
                // and only increase the capacity by that amount...
                mutatedMatches.EnsureCapacity(mutatedMatches.Count + this.mutated.Count);
#endif

                foreach (var documentMutation in this.mutated)
                {
                    mutatedMatches[documentMutation.Key] = documentMutation.Value;
                }
            }

            return new DocumentTokenMatchMap(mutatedMatches);
        }

        public int MutationCount => this.mutated?.Count ?? 0;

        public void Remove(int documentId)
        {
            if (this.removed == null)
            {
                this.removed = [documentId];
            }
            else
            {
                this.removed.Add(documentId);
            }

            // It's technically possible for a document to be added to the index, and in the same mutation removed
            // again. In this case, we can just remove it from the mutations dictionary as if it was never
            // added to it.
            this.mutated?.Remove(documentId);
        }

        internal void Add(int documentId, IndexedToken indexedToken)
        {
            this.mutated ??= [];

            if (this.mutated.TryGetValue(documentId, out var documentFieldLocations))
            {
                // The field locations list will already have been cloned when it was added to the mutations dictionary
                // so it's safe to just add to it here
                documentFieldLocations.Add(indexedToken);
            }
            else
            {
                documentFieldLocations = this.original.StartMutation(documentId);
                documentFieldLocations.Add(indexedToken);
                this.mutated.Add(documentId, documentFieldLocations);
            }
        }
    }
}