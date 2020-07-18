using System;

namespace Lifti.Querying
{
    /// <summary>
    /// A factory class for <see cref="OkapiBm25Scorer"/> instances.
    /// </summary>
    public class OkapiBm25ScorerFactory : IIndexScorerFactory
    {
        private readonly double k1;
        private readonly double b;

        /// <summary>
        /// Constructs a new instance of the <see cref="OkapiBm25ScorerFactory"/> class.
        /// </summary>
        /// <param name="k1">The "k1" parameter for the scorer.</param>
        /// <param name="b">The "b" parameter for the scorer.</param>
        public OkapiBm25ScorerFactory(double k1 = 1.2D, double b = 0.75D)
        {
            this.k1 = k1;
            this.b = b;
        }

        /// <inheritdoc />
        public IScorer CreateIndexScorer(IIndexSnapshot indexSnapshot)
        {
            if (indexSnapshot is null)
            {
                throw new ArgumentNullException(nameof(indexSnapshot));
            }

            return new OkapiBm25Scorer(this.k1, this.b,  indexSnapshot.Items);
        }
    }
}
