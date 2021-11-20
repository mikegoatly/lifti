using System;

namespace Lifti.Querying.QueryParts
{
    /// <inheritdoc />
    public abstract class WordQueryPart : IQueryPart
    {
        /// <summary>
        /// Constructs a new instance of <see cref="WordQueryPart"/>.
        /// </summary>
        protected WordQueryPart(string word)
        {
            this.Word = word;
        }

        /// <inheritdoc/>
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
