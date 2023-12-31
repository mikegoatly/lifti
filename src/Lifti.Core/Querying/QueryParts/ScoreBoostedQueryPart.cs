using System;

namespace Lifti.Querying.QueryParts
{
    /// <inheritdoc />
    public abstract class ScoreBoostedQueryPart : IQueryPart
    {
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

        /// <inheritdoc />
        public abstract IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext);

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
