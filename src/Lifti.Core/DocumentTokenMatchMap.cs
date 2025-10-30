using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lifti
{
    /// <summary>
    /// A read only map of <see cref="IndexedToken"/>s keyed by the internal document id.
    /// </summary>
    public readonly struct DocumentTokenMatchMap : IEquatable<DocumentTokenMatchMap>
    {
        internal DocumentTokenMatchMap(IEnumerable<KeyValuePair<int, IReadOnlyList<IndexedToken>>> data)
        {
            this.DocumentTokenLookup = new(data);
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