using Lifti.Querying;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tests.Fakes
{
    public class FakeScorerFactory : IIndexScorerFactory
    {
        private readonly IScorer scorer;

        public FakeScorerFactory(IScorer scorer)
        {
            this.scorer = scorer;
        }

        public IScorer CreateIndexScorer(IIndexSnapshot indexSnapshot)
        {
            return this.scorer;
        }
    }
}
