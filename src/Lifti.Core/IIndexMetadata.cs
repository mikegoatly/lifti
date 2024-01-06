﻿using System;
using System.Collections.Generic;

namespace Lifti
{
    /// <summary>
    /// Describes methods for accessing metadata information about an index.
    /// </summary>
    public interface IIndexMetadata
    {
        /// <summary>
        /// Gets the number of documents in the index.
        /// </summary>
        [Obsolete("Use DocumentCount property instead")]
        int Count { get; }

        /// <summary>
        /// Gets the number of documents in the index.
        /// </summary>
        int DocumentCount { get; }

        /// <summary>
        /// Gets the <see cref="DocumentMetadata"/> for the given internal document id.
        /// </summary>
        /// <exception cref="LiftiException">
        /// Thrown when the id is not known.
        /// </exception>
        DocumentMetadata GetDocumentMetadata(int documentId);

        /// <inheritdoc cref="GetDocumentMetadata(int)"/>
        [Obsolete("Use GetDocumentMetadata(int) instead")]
        DocumentMetadata GetMetadata(int documentId);

        /// <summary>
        /// Gets the calculated <see cref="ScoreBoostMetadata"/> for the given object type. This can be used
        /// to determine the score boost for an instance of <see cref="DocumentMetadata"/>.
        /// </summary>
        ScoreBoostMetadata GetObjectTypeScoreBoostMetadata(byte objectTypeId);

        /// <summary>
        /// Gets the aggregated statistics for all the indexed documents, including total token count.
        /// </summary>
        IndexStatistics IndexStatistics { get; }
    }

    /// <summary>
    /// Describes methods for accessing metadata information about an index.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of the key in the index.
    /// </typeparam>
    public interface IIndexMetadata<TKey> : IIndexMetadata
    {
        /// <summary>
        /// Enumerates each <see cref="DocumentMetadata{TKey}"/> in the index.
        /// </summary>
        IEnumerable<DocumentMetadata<TKey>> GetIndexedDocuments();

        /// <summary>
        /// Gets a value indicating whether the given key has been added to the index.
        /// </summary>
        bool Contains(TKey key);

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        /// <summary>
        /// Gets the <see cref="DocumentMetadata{TKey}"/> for the given document id.
        /// </summary>
        /// <exception cref="LiftiException">
        /// Thrown when the id is not known.
        /// </exception>
        DocumentMetadata<TKey> GetDocumentMetadata(int documentId);

        /// <inheritdoc cref="GetDocumentMetadata(int)"/>
        [Obsolete("Use GetDocumentMetadata(int) instead")]
        DocumentMetadata<TKey> GetMetadata(int documentId);
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

        /// <summary>
        /// Gets the <see cref="DocumentMetadata{TKey}"/> for the given key.
        /// </summary>
        /// <exception cref="LiftiException">
        /// Thrown when the key is not known.
        /// </exception>
        DocumentMetadata<TKey> GetMetadata(TKey key);
    }
}