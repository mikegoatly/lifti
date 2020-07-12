using System;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// A query part that matches on a single word in some way.
    /// </summary>
    public abstract class WordQueryPart : IWordQueryPart
    {
        protected WordQueryPart(string word)
        {
            this.Word = word;
        }

        public string Word
        {
            get;
        }

        /// <summary>
        /// Evaluates this instance against the index within the given <see cref="IQueryContext"/>, returning an <see cref="IntermediateQueryResult"/>
        /// that contains the matches.
        /// </summary>
        public abstract IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, IQueryContext queryContext);
    }
}
