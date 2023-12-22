using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
            var remainingOriginalMatches = this.removed == null
                ? this.original.Enumerate()
                : this.original.Enumerate().Where(x => !this.removed.Contains(x.documentId));

            Dictionary<int, IReadOnlyList<IndexedToken>> mutatedMatches;
            if (this.mutated == null)
            {
                // Just create a new dictionary with the remaining original matches
                mutatedMatches = remainingOriginalMatches.ToDictionary(x => x.documentId, x => x.indexedTokens);
            }
            else
            {
                // Copy any unmutated matches that haven't been removed into the mutated matches dictionary
                mutatedMatches = this.mutated.ToDictionary(x => x.Key, x => (IReadOnlyList<IndexedToken>)x.Value);
                foreach (var (documentId, indexedTokens) in remainingOriginalMatches)
                {
#if NETSTANDARD
                    if (!mutatedMatches.ContainsKey(documentId))
                    {
                        mutatedMatches.Add(documentId, indexedTokens);
                    }
#else
                    mutatedMatches.TryAdd(documentId, indexedTokens);
#endif
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

            if (this.mutated.TryGetValue(documentId, out var itemFieldLocations))
            {
                // The field locations list will already have been cloned when it was added to the mutations dictionary
                // so it's safe to just add to it here
                itemFieldLocations.Add(indexedToken);
            }
            else
            {
                itemFieldLocations = this.original.StartMutation(documentId);
                itemFieldLocations.Add(indexedToken);
                this.mutated.Add(documentId, itemFieldLocations);
            }
        }
    }

    /// <summary>
    /// A read only map of <see cref="IndexedToken"/>s keyed by the internal item id.
    /// </summary>
    public readonly struct DocumentTokenMatchMap : IEquatable<DocumentTokenMatchMap>
    {
        private readonly Dictionary<int, IReadOnlyList<IndexedToken>> documentTokens;

        internal DocumentTokenMatchMap(IEnumerable<KeyValuePair<int, IReadOnlyList<IndexedToken>>> data)
        {
#if NETSTANDARD
            this.documentTokens = data.ToDictionary(x => x.Key, x => x.Value);
#else
            this.documentTokens = new(data);
#endif
        }

        internal DocumentTokenMatchMap(Dictionary<int, IReadOnlyList<IndexedToken>> data)
        {
            this.documentTokens = data;
        }

        /// <summary>
        /// Gets an empty <see cref="DocumentTokenMatchMap"/>.
        /// </summary>
        public static DocumentTokenMatchMap Empty { get; } = new DocumentTokenMatchMap(Array.Empty<KeyValuePair<int, IReadOnlyList<IndexedToken>>>());

        /// <summary>
        /// Gets the number of documents in the map.
        /// </summary>
        public int Count => this.documentTokens.Count;

        /// <summary>
        /// Enumerates all the document matches in the map.
        /// </summary>
        public IEnumerable<(int documentId, IReadOnlyList<IndexedToken> indexedTokens)> Enumerate()
        {
            foreach (var document in this.documentTokens)
            {
                yield return (document.Key, document.Value);
            }
        }

        /// <summary>
        /// Tries to get the list of indexed tokens for the specified document.
        /// </summary>
        public bool TryGetValue(int documentId, [NotNullWhen(true)] out IReadOnlyList<IndexedToken>? tokens)
        {
            return this.documentTokens.TryGetValue(documentId, out tokens);
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
            if (this.documentTokens.TryGetValue(documentId, out var indexedTokens))
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
            return this.documentTokens.ContainsKey(documentId);
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
            return this.documentTokens == other.documentTokens;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.documentTokens.GetHashCode();
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
