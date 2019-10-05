using System;
using System.Collections.Generic;

namespace Lifti.Querying.QueryParts
{
    public class AdjacentWordsQueryOperator : IQueryPart
    {
        public AdjacentWordsQueryOperator(IReadOnlyList<IWordQueryPart> words)
        {
            this.Words = words;
        }

        public IReadOnlyList<IWordQueryPart> Words { get; }

        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator)
        {
            var i = 0;
            var results = IntermediateQueryResult.Empty;
            do
            {
                var nextResults = this.Words[i].Evaluate(navigatorCreator);
                if (results.Matches.Count == 0)
                {
                    results = nextResults;
                }
                else
                {
                    results = results.CompositePositionalIntersect(nextResults, 0, 1);
                }

                i++;

            } while (i < this.Words.Count && results.Matches.Count > 0);

            return results;
        }

        public override string ToString()
        {
            return "\"" + string.Join(" ", this.Words) + "\"";
        }
    }
}
