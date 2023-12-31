using System;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// An <see cref="IQueryPart"/> that only matches documents that contain an exact match for the given text.
    /// </summary>
    public class ExactWordQueryPart : WordQueryPart
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

            using var navigator = navigatorCreator();
            navigator.Process(this.Word.AsSpan());
            return queryContext.ApplyTo(navigator.GetExactMatches(this.ScoreBoost ?? 1D));
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return base.ToString(this.Word);
        }
    }
}
