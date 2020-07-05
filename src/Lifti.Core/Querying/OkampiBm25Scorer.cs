using System;

namespace Lifti.Querying
{
    public class OkampiBm25Scorer : IScorer
    {
        private readonly double k1;
        private readonly double b;

        public OkampiBm25Scorer(double k1 = 1.2D, double b = 0.75D)
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

            return new OkampiBm25IndexScorer(this.k1, this.b,  indexSnapshot.Items);
        }
    }
}
