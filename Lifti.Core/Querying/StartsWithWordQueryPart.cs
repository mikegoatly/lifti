using System;

namespace Lifti.Querying
{
    /// <summary>
    /// A query part that matches items that are indexed starting with given text.
    /// </summary>
    public class StartsWithWordQueryPart : WordQueryPart
    {
        public StartsWithWordQueryPart(string word)
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
            return navigator.GetExactAndChildMatches();
        }

        public override string ToString()
        {
            return "STARTSWITH(" + this.Word + ")";
        }
    }
}
