using System;

namespace Lifti
{
    /// <summary>
    /// Describes metadata for an indexed document.
    /// </summary>
    public abstract class DocumentMetadata(
        byte? objectTypeId,
        int documentId,
        DocumentStatistics documentStatistics,
        DateTime? scoringFreshnessDate,
        double? scoringMagnitude)
    {
        /// <summary>
        /// Gets the id of the object type configured for the indexed document. This will be null if the document source was loose
        /// indexed text, or the index was deserialized from an older version without object type id awareness.
        /// </summary>
        public byte? ObjectTypeId { get; } = objectTypeId;

        /// <summary>
        /// Gets the document ID of the document used internally in the index.
        /// </summary>
        public int Id { get; } = documentId;

        /// <summary>
        /// Gets the statistics for the indexed document, including token count.
        /// </summary>
        public DocumentStatistics DocumentStatistics { get; } = documentStatistics;

        /// <summary>
        /// Gets the freshness date of the indexed document for scoring purposes, if one was specified.
        /// </summary>
        public DateTime? ScoringFreshnessDate { get; } = scoringFreshnessDate;

        /// <summary>
        /// Gets the magnitude weighting for the indexed document, if one was specified.
        /// </summary>
        public double? ScoringMagnitude { get; } = scoringMagnitude;
    }

    /// <inheritdoc cref="DocumentMetadata" />
    /// <typeparam name="TKey">The type of the key in the index.</typeparam>
    public class DocumentMetadata<TKey> : DocumentMetadata
    {
        /// <summary>
        /// Gets the key of the indexed document.
        /// </summary>
        [Obsolete("Use Key property instead")]
        public TKey Item => this.Key;

        /// <summary>
        /// Gets the key of the indexed document.
        /// </summary>
        public TKey Key { get; }

        private DocumentMetadata(
            int documentId,
            TKey key,
            DocumentStatistics documentStatistics,
            byte? objectTypeId = null,
            DateTime? scoringFreshnessDate = null,
            double? scoringMagnitude = null)
            : base(objectTypeId, documentId, documentStatistics, scoringFreshnessDate, scoringMagnitude)
        {
            this.Key = key;
        }

        internal static DocumentMetadata<TKey> ForLooseText(int documentId, TKey key, DocumentStatistics documentStatistics)
        {
            return new DocumentMetadata<TKey>(documentId, key, documentStatistics);
        }

        internal static DocumentMetadata<TKey> ForObject(
            byte objectTypeId,
            int documentId,
            TKey key,
            DocumentStatistics documentStatistics,
            DateTime? scoringFreshnessDate,
            double? scoringMagnitude)
        {
            return new DocumentMetadata<TKey>(documentId, key, documentStatistics, objectTypeId, scoringFreshnessDate, scoringMagnitude);
        }
    }
}