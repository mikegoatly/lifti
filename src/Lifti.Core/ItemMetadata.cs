using System;

namespace Lifti
{
    /// <summary>
    /// Describes metadata for an indexed item.
    /// </summary>
    public abstract class ItemMetadata(
        byte? objectTypeId,
        int id,
        DocumentStatistics documentStatistics,
        DateTime? scoringFreshnessDate,
        double? scoringMagnitude)
    {
        /// <summary>
        /// Gets the id of the object type configured for the indexed item. Will be null if the item was just loose
        /// indexed text, or the index was deserialized from an older version without object type id awareness.
        /// </summary>
        public byte? ObjectTypeId { get; } = objectTypeId;

        /// <summary>
        /// Gets the ID of the indexed item used internally in the index.
        /// </summary>
        public int Id { get; } = id;

        /// <summary>
        /// Gets the statistics for the indexed document, including token count.
        /// </summary>
        public DocumentStatistics DocumentStatistics { get; } = documentStatistics;

        /// <summary>
        /// Gets the freshness date of the indexed item for scoring purposes, if one was specified.
        /// </summary>
        public DateTime? ScoringFreshnessDate { get; } = scoringFreshnessDate;

        /// <summary>
        /// Gets the magnitude weighting for the indexed item, if one was specified.
        /// </summary>
        public double? ScoringMagnitude { get; } = scoringMagnitude;
    }

    /// <inheritdoc cref="ItemMetadata" />
    /// <typeparam name="TKey">The type of the key in the index.</typeparam>
    public class ItemMetadata<TKey> : ItemMetadata
    {
        /// <summary>
        /// Gets the indexed item.
        /// </summary>
        [Obsolete("Use Key property instead")]
        public TKey Item => this.Key;

        /// <summary>
        /// Gets the key of the indexed item.
        /// </summary>
        public TKey Key { get; }

        private ItemMetadata(
            int itemId,
            TKey key,
            DocumentStatistics documentStatistics,
            byte? objectTypeId = null,
            DateTime? scoringFreshnessDate = null,
            double? scoringMagnitude = null)
            : base(objectTypeId, itemId, documentStatistics, scoringFreshnessDate, scoringMagnitude)
        {
            this.Key = key;
        }

        internal static ItemMetadata<TKey> ForLooseText(int itemId, TKey key, DocumentStatistics documentStatistics)
        {
            return new ItemMetadata<TKey>(itemId, key, documentStatistics);
        }

        internal static ItemMetadata<TKey> ForObject(
            byte objectTypeId,
            int itemId,
            TKey key,
            DocumentStatistics documentStatistics,
            DateTime? scoringFreshnessDate,
            double? scoringMagnitude)
        {
            return new ItemMetadata<TKey>(itemId, key, documentStatistics, objectTypeId, scoringFreshnessDate, scoringMagnitude);
        }
    }
}