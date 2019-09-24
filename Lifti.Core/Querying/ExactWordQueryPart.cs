using System;

namespace Lifti.Querying
{
    /// <summary>
    /// A query part that matches items that are indexed against a single word.
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

            var navigator = navigatorCreator();
            navigator.Process(this.Word.AsSpan());
            return navigator.GetExactMatches();
        }

        public override string ToString()
        {
            return "EXACT(" + this.Word + ")";
        }
    }
}
