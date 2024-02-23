using System;

namespace Lifti.Querying.QueryParts
{
    /// <inheritdoc />
    public abstract class ScoreBoostedQueryPart : IQueryPart
    {
        private double? weighting;

        /// <summary>
        /// Constructs a new instance of <see cref="ScoreBoostedQueryPart"/>.
        /// </summary>
        /// <param name="scoreBoost">
        /// The score boost to apply to any matches that this query part finds. This is multiplied with any score boosts
        /// applied to matching fields. A null value indicates that no additional score boost should be applied.
        /// </param>
        protected ScoreBoostedQueryPart(double? scoreBoost)
        {
            this.ScoreBoost = scoreBoost;
        }

        /// <summary>
        /// The score boost to apply to any matches that this query part finds. This is multiplied with any score boosts
        /// applied to matching fields. A null value indicates that no additional score boost should be applied.
        /// </summary>
        public double? ScoreBoost { get; }

        /// <summary>
        /// Gets the weighting calculated for this query part. If the weighting did not need to be calculated, this will
        /// be <c>null</c>.
        /// </summary>
        internal double? CalculatedWeighting => this.weighting;

        /// <inheritdoc />
        public double CalculateWeighting(Func<IIndexNavigator> navigatorCreator)
        {
            this.weighting ??= this.RunWeightingCalculation(navigatorCreator);
            return this.weighting.GetValueOrDefault();
        }

        /// <inheritdoc />
        public abstract IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext);

        /// <summary>
        /// Runs the weighting calculation for this query part.
        /// </summary>
        protected abstract double RunWeightingCalculation(Func<IIndexNavigator> navigatorCreator);

        /// <summary>
        /// Returns a string representation of this query part.
        /// </summary>
        protected virtual string ToString(string searchTerm)
        {
            if (this.ScoreBoost.HasValue)
            {
                return $"{searchTerm}^{this.ScoreBoost.Value}";
            }

            return searchTerm;
        }
    }
}
