using System;
using System.Collections.Concurrent;

namespace Lifti
{
    internal sealed class SharedPool<T>
        where T : notnull
    {
        private readonly ConcurrentBag<T> pool = [];
        private readonly Func<T> createNew;
        private readonly Action<T> resetForReuse;
        private readonly int maxCapacity;

        public SharedPool(Func<T> createNew, Action<T> resetForReuse, int maxCapacity = 10)
        {
            this.createNew = createNew;
            this.resetForReuse = resetForReuse;
            this.maxCapacity = maxCapacity;
        }

        public T Take()
        {
            if (!this.pool.TryTake(out var result))
            {
                result = this.createNew();
            }

            return result;
        }

        public void Return(T reusable)
        {
            if (this.pool.Count > this.maxCapacity)
            {
                return;
            }

            this.resetForReuse(reusable);
            this.pool.Add(reusable);
        }
    }
}
