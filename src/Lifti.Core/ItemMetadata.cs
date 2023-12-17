using System;

namespace Lifti
{
    /// <summary>
    /// Describes metadata for an indexed item.
    /// </summary>
    public abstract class ItemMetadata(
        int id,
        DocumentStatistics documentStatistics,
        DateTime? scoringFreshnessDate,
        double? scoringMagnitude)
    {
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
    public class ItemMetadata<TKey>(
        int id,
        TKey item,
        DocumentStatistics documentStatistics,
        DateTime? scoringFreshnessDate,
        double? scoringMagnitude)
        : ItemMetadata(id, documentStatistics, scoringFreshnessDate, scoringMagnitude)
    {

        /// <summary>
        /// Gets the indexed item.
        /// </summary>
        [Obsolete("Use Key property instead")]
        public TKey Item => this.Key;

        /// <summary>
        /// Gets the key of the indexed item.
        /// </summary>
        public TKey Key { get; } = item;
    }
}