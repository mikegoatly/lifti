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


        public abstract IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, IQueryContext queryContext);
    }
}
