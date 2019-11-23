using System;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// A query part that matches items that are indexed exactly against text.
    /// </summary>
    public class ExactWordQueryPart : WordQueryPart
    {
        public ExactWordQueryPart(string word)
            : base(word)
        {
        }

        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator)
        {
            if (navigatorCreator == null)
            {
                throw new ArgumentNullException(nameof(navigatorCreator));
            }

            using (var navigator = navigatorCreator())
            {
                navigator.Process(this.Word.AsSpan());
                return navigator.GetExactMatches();
            }
        }

        public override string ToString()
        {
            return this.Word;
        }
    }
}
