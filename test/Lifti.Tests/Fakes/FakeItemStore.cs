using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tests.Fakes
{
    internal class FakeItemStore<T> : IItemStore
    {
        private readonly Dictionary<int, ItemMetadata<T>> itemMetadata;

        public FakeItemStore(int count, IndexStatistics statistics, params (int id, ItemMetadata<T> statistics)[] itemMetadata)
        {
            this.Count = count;
            this.IndexStatistics = statistics;
            this.itemMetadata = itemMetadata.ToDictionary(i => i.id, i => i.statistics);
        }

        public int Count { get; private set; }

        public IndexStatistics IndexStatistics { get; private set; }

        public ItemMetadata GetMetadata(int id)
        {
            return this.itemMetadata[id];
        }
    }
}
