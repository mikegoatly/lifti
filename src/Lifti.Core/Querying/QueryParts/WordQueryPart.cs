using System;

namespace Lifti.Querying.QueryParts
{

    /// <inheritdoc />
    public abstract class WordQueryPart : ScoreBoostedQueryPart
    {
        /// <summary>
        /// Constructs a new instance of <see cref="WordQueryPart"/>.
        /// </summary>
        protected WordQueryPart(string word, double? scoreBoost)
            : base(scoreBoost)
        {
            this.Word = word;
        }

        /// <inheritdoc/>
        public string Word
        {
            get;
        }

        /// <inheritdoc/>
        protected override double RunWeightingCalculation(Func<IIndexNavigator> navigatorCreator)
        {
            if (navigatorCreator is null)
            {
                throw new ArgumentNullException(nameof(navigatorCreator));
            }

            using var navigator = navigatorCreator();
            navigator.Process(this.Word.AsSpan());

            var totalDocumentCount = navigator.Snapshot.Metadata.DocumentCount;
            if (totalDocumentCount == 0)
            {
                // Edge case for an empty index
                return 0;
            }

            return navigator.ExactMatchCount() / (double)totalDocumentCount;
        }
    }
}
