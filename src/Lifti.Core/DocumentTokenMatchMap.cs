using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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

    /// <summary>
    /// A read only map of <see cref="IndexedToken"/>s keyed by the internal document id.
    /// </summary>
    public readonly struct DocumentTokenMatchMap : IEquatable<DocumentTokenMatchMap>
    {
        internal DocumentTokenMatchMap(IEnumerable<KeyValuePair<int, IReadOnlyList<IndexedToken>>> data)
        {
#if NETSTANDARD
            this.DocumentTokenLookup = data.ToDictionary(x => x.Key, x => x.Value);
#else
            this.DocumentTokenLookup = new(data);
#endif
        }

        /// <summary>
        /// Constructs a new instance of <see cref="DocumentTokenMatchMap"/>.
        /// </summary>
        /// <param name="data">
        /// A dictionary of document id to indexed tokens to initialize the map with.
        /// </param>
        public DocumentTokenMatchMap(Dictionary<int, IReadOnlyList<IndexedToken>> data)
        {
            this.DocumentTokenLookup = data;
        }

        /// <summary>
        /// Gets an empty <see cref="DocumentTokenMatchMap"/>.
        /// </summary>
        public static DocumentTokenMatchMap Empty { get; } = new DocumentTokenMatchMap(Array.Empty<KeyValuePair<int, IReadOnlyList<IndexedToken>>>());

        internal Dictionary<int, IReadOnlyList<IndexedToken>> DocumentTokenLookup { get; }

        /// <summary>
        /// Gets the number of documents in the map.
        /// </summary>
        public int Count => this.DocumentTokenLookup.Count;

        /// <summary>
        /// Enumerates all the document matches in the map.
        /// </summary>
        public IEnumerable<(int documentId, IReadOnlyList<IndexedToken> indexedTokens)> Enumerate()
        {
            foreach (var document in this.DocumentTokenLookup)
            {
                yield return (document.Key, document.Value);
            }
        }

        /// <summary>
        /// Tries to get the list of indexed tokens for the specified document.
        /// </summary>
        public bool TryGetValue(int documentId, [NotNullWhen(true)] out IReadOnlyList<IndexedToken>? tokens)
        {
            return this.DocumentTokenLookup.TryGetValue(documentId, out tokens);
        }

        /// <summary>
        /// Begins mutation the list of indexed tokens for the specified document. If the document is not already
        /// indexed at this node, a new empty list will be created. If the document is already indexed at this node,
        /// a clone of the list will be created and returned, so is safe to be mutated.
        /// </summary>
        /// <param name="documentId"></param>
        /// <returns></returns>
        internal List<IndexedToken> StartMutation(int documentId)
        {
            if (this.DocumentTokenLookup.TryGetValue(documentId, out var indexedTokens))
            {
                return new List<IndexedToken>(indexedTokens);
            }
            else
            {
                return [];
            }
        }

        /// <summary>
        /// Gets a value indicating whether the map contains any matches for the specified document.
        /// </summary>
        public bool HasDocument(int documentId)
        {
            return this.DocumentTokenLookup.ContainsKey(documentId);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is DocumentTokenMatchMap other
                && this.Equals(other);
        }

        /// <inheritdoc/>
        public bool Equals(DocumentTokenMatchMap other)
        {
            // Because we're immutable, we can just compare the references
            return this.DocumentTokenLookup == other.DocumentTokenLookup;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.DocumentTokenLookup.GetHashCode();
        }

        /// <inheritdoc/>
        public static bool operator ==(DocumentTokenMatchMap left, DocumentTokenMatchMap right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(DocumentTokenMatchMap left, DocumentTokenMatchMap right)
        {
            return !(left == right);
        }
    }
}