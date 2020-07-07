using System;

namespace Lifti.Querying
{
    public class OkapiBm25Scorer : IScorer
    {
        private readonly double k1;
        private readonly double b;

        public OkapiBm25Scorer(double k1 = 1.2D, double b = 0.75D)
        {
            this.k1 = k1;
            this.b = b;
        }

        public IIndexScorer CreateIndexScorer(IIndexSnapshot indexSnapshot)
        {
            if (indexSnapshot is null)
            {
                throw new ArgumentNullException(nameof(indexSnapshot));
            }

            return new OkapiBm25IndexScorer(this.k1, this.b,  indexSnapshot.Items);
        }
    }
}
