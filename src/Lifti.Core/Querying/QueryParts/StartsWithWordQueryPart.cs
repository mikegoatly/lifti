using System;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// An <see cref="IQueryPart"/> that matches items that are indexed starting with given text.
    /// </summary>
    public class StartsWithWordQueryPart : WordQueryPart
    {
        /// <summary>
        /// Constructs a new instance of <see cref="StartsWithWordQueryPart"/>.
        /// </summary>
        /// <param name="word"></param>
        public StartsWithWordQueryPart(string word)
            : base(word)
        {
        }

        /// <inheritdoc/>
        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, IQueryContext queryContext)
        {
            if (navigatorCreator == null)
            {
                throw new ArgumentNullException(nameof(navigatorCreator));
            }

            using (var navigator = navigatorCreator())
            {
                navigator.Process(this.Word.AsSpan());
                return queryContext.ApplyTo(navigator.GetExactAndChildMatches());
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Word}*";
        }
    }
}
