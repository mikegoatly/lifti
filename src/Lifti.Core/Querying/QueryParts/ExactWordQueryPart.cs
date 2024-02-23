using System;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// An <see cref="IQueryPart"/> that only matches documents that contain an exact match for the given text.
    /// </summary>
    public sealed class ExactWordQueryPart : WordQueryPart
    {
        /// <summary>
        /// Constructs a new instance of <see cref="ExactWordQueryPart"/>.
        /// </summary>
        public ExactWordQueryPart(string word, double? scoreBoost = null)
            : base(word, scoreBoost)
        {
        }

        /// <inheritdoc/>
        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext)
        {
            if (navigatorCreator == null)
            {
                throw new ArgumentNullException(nameof(navigatorCreator));
            }

            if (queryContext is null)
            {
                throw new ArgumentNullException(nameof(queryContext));
            }

            var timing = queryContext.ExecutionTimings.Start(this, queryContext);
            using var navigator = navigatorCreator();
            navigator.Process(this.Word.AsSpan());
            var results = navigator.GetExactMatches(queryContext, this.ScoreBoost ?? 1D);
            return timing.Complete(results);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return base.ToString(this.Word);
        }
    }
}
