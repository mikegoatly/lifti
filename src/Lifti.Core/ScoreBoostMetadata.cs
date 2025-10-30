using Lifti.Tokenization.Objects;
using System;

namespace Lifti
{
    /// <summary>
    /// Maintains information about all the scoring metadata, e.g. freshness date, magnitude values, encountered
    /// for a single object type.
    /// </summary>
    public class ScoreBoostMetadata
    {
        private readonly DateTimeScoreBoostValues freshnessBoost = new();
        private readonly DoubleScoreBoostValues magnitudeBoost = new();
        private readonly ObjectScoreBoostOptions scoreBoostOptions;

        internal ScoreBoostMetadata(ObjectScoreBoostOptions scoreBoostOptions)
        {
            this.scoreBoostOptions = scoreBoostOptions;
        }

        /// <summary>
        /// Calculates the score boost for the given <see cref="DocumentMetadata"/>.
        /// </summary>
        /// <remarks>
        /// This is virtual to allow for unit tests to override its behavior.
        /// </remarks>
        public virtual double CalculateScoreBoost(DocumentMetadata documentMetadata)
        {
            ArgumentNullException.ThrowIfNull(documentMetadata);

            if (documentMetadata.ScoringFreshnessDate is null && documentMetadata.ScoringMagnitude is null)
            {
                return 1.0D;
            }

            if (this.freshnessBoost is null || this.magnitudeBoost is null)
            {
                throw new LiftiException(ExceptionMessages.ScoreBoostsNotCalculated);
            }

            return this.freshnessBoost.CalculateBoost(this.scoreBoostOptions.FreshnessMultiplier, documentMetadata.ScoringFreshnessDate)
                + this.magnitudeBoost.CalculateBoost(this.scoreBoostOptions.MagnitudeMultiplier, documentMetadata.ScoringMagnitude);
        }

        internal void Add(DocumentMetadata documentMetadata)
        {
            AddToBoostValues(this.freshnessBoost, documentMetadata.ScoringFreshnessDate);
            AddToBoostValues(this.magnitudeBoost, documentMetadata.ScoringMagnitude);
        }

        internal void Remove(DocumentMetadata documentMetadata)
        {
            RemoveFromBoostValues(this.freshnessBoost, documentMetadata.ScoringFreshnessDate);
            RemoveFromBoostValues(this.magnitudeBoost, documentMetadata.ScoringMagnitude);
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
