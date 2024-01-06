﻿using System.Collections.Concurrent;

namespace Lifti.Querying
{
    internal sealed class IndexNavigatorPool : IIndexNavigatorPool
    {
        private readonly ConcurrentBag<IndexNavigator> pool = [];
        private readonly IIndexScorerFactory scorer;

        public IndexNavigatorPool(IIndexScorerFactory scorer)
        {
            this.scorer = scorer;
        }

        public IIndexNavigator Create(IIndexSnapshot indexSnapshot)
        {
            if (!this.pool.TryTake(out var navigator))
            {
                navigator = new IndexNavigator();
            }

            navigator.Initialize(indexSnapshot, this, this.scorer.CreateIndexScorer(indexSnapshot));
            return navigator;
        }

        public void Return(IndexNavigator navigator)
        {
            if (this.pool.Count > 10)
            {
                return;
            }

            // TODO should there be any consideration to navigators that have had their
            // string build grown to a large size? Should they not be returned to the pool so they can be GCed?
            this.pool.Add(navigator);
        }
    }
}
