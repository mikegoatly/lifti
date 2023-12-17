using Lifti.Tokenization.Objects;
using System;

namespace Lifti
{
    /// <summary>
    /// Information about the range of scoring data, e.g. freshness date, magnitude values, encountered
    /// for a single object type.
    /// </summary>
    public class ObjectScoreBoostMetadata
    {
        private readonly DateTimeScoreBoostValues freshnessBoost = new();
        private readonly DoubleScoreBoostValues magnitudeBoost = new();
        private readonly ObjectScoreBoostOptions scoreBoostOptions;

        internal ObjectScoreBoostMetadata(ObjectScoreBoostOptions scoreBoostOptions)
        {
            this.scoreBoostOptions = scoreBoostOptions;
        }

        /// <summary>
        /// Calculates the score boost for the given item.
        /// </summary>
        public double CalculateScoreBoost(ItemMetadata itemMetadata)
        {
            if (itemMetadata is null)
            {
                throw new ArgumentNullException(nameof(itemMetadata));
            }

            if (itemMetadata.ScoringFreshnessDate is null && itemMetadata.ScoringMagnitude is null)
            {
                return 1.0D;
            }

            if (this.freshnessBoost is null || this.magnitudeBoost is null)
            {
                throw new LiftiException(ExceptionMessages.ScoreBoostsNotCalculated);
            }

            return this.freshnessBoost.CalculateBoost(this.scoreBoostOptions.FreshnessMultiplier, itemMetadata.ScoringFreshnessDate)
                + this.magnitudeBoost.CalculateBoost(this.scoreBoostOptions.MagnitudeMultiplier, itemMetadata.ScoringMagnitude);
        }

        internal void Add(ItemMetadata itemMetadata)
        {
            AddToBoostValues(this.freshnessBoost, itemMetadata.ScoringFreshnessDate);
            AddToBoostValues(this.magnitudeBoost, itemMetadata.ScoringMagnitude);
        }

        internal void Remove(ItemMetadata itemMetadata)
        {
            RemoveFromBoostValues(this.freshnessBoost, itemMetadata.ScoringFreshnessDate);
            RemoveFromBoostValues(this.magnitudeBoost, itemMetadata.ScoringMagnitude);
        }

        private static void RemoveFromBoostValues<T>(ScoreBoostValues<T> boostValues, T? newValue)
            where T : struct, IComparable<T>
        {
            if (newValue is null)
            {
                // Nothing to do
                return;
            }

            var value = newValue.GetValueOrDefault();
            boostValues.Remove(value);
        }

        private static void AddToBoostValues<T>(ScoreBoostValues<T> boostValues, T? newValue)
            where T : struct, IComparable<T>
        {
            if (newValue is null)
            {
                // Nothing to do
                return;
            }

            var value = newValue.GetValueOrDefault();
            boostValues.Add(value);
        }
    }
}
