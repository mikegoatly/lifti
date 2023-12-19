using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tests.Fakes
{
    internal class FakeItemStore<TKey> : IItemStore
    {
        private readonly Dictionary<int, ItemMetadata<TKey>> itemMetadata;
        private readonly Dictionary<byte, Func<ItemMetadata, double>> objectTypeMetadata;

        public FakeItemStore(
            int count, 
            IndexStatistics statistics, 
            (int documentId, ItemMetadata<TKey> statistics)[] documentMetadata,
            (byte objectTypeId, Func<ItemMetadata, double> scoreProvider)[] objectTypeMetadata)
        {
            this.Count = count;
            this.IndexStatistics = statistics;
            this.itemMetadata = documentMetadata.ToDictionary(i => i.documentId, i => i.statistics);
            this.objectTypeMetadata = objectTypeMetadata.ToDictionary(i => i.objectTypeId, i => i.scoreProvider);
        }

        public int Count { get; private set; }

        public IndexStatistics IndexStatistics { get; private set; }

        public ItemMetadata GetMetadata(int id)
        {
            return this.itemMetadata[id];
        }

        public ScoreBoostMetadata GetObjectTypeScoreBoostMetadata(byte objectTypeId)
        {
            return new FakeScoreBoostMetadata(this.objectTypeMetadata[objectTypeId]);
        }

        private class FakeScoreBoostMetadata : ScoreBoostMetadata
        {
            private Func<ItemMetadata, double> scoreBoostCalculator;

            public FakeScoreBoostMetadata(Func<ItemMetadata, double> func)
                : base(null!)
            {
                this.scoreBoostCalculator = func;
            }

            public override double CalculateScoreBoost(ItemMetadata itemMetadata)
            {
                return this.scoreBoostCalculator(itemMetadata);
            }
        }
    }
}
