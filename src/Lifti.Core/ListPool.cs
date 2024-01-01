using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Lifti
{
    internal sealed class ListPool<T>
    {
        private readonly ConcurrentBag<List<T>> pool = [];
        private readonly int defaultLength;
        private readonly int maxPoolSize;
        private readonly int maxReturnSize;

        public ListPool(int defaultLength, int maxPoolSize, int maxReturnSize)
        {
            this.defaultLength = defaultLength;
            this.maxPoolSize = maxPoolSize;
            this.maxReturnSize = maxReturnSize;
        }

        public static ListPool<T> Default { get; } = new(10, 10, 1000);

        public List<T> Take()
        {
            if (this.pool.TryTake(out var list))
            {
                return list;
            }

            return new(this.defaultLength);
        }

        public void Return(List<T> list)
        {
            list.Clear();

            if (list.Capacity > this.maxReturnSize || this.pool.Count >= this.maxPoolSize)
            {
                return;
            }

            this.pool.Add(list);
        }
    }
}
