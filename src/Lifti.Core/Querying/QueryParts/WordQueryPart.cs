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
    }
}
