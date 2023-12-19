using System;
using System.Collections.Concurrent;

namespace Lifti
{
    internal class SharedPool<T>
        where T : notnull
    {
        private readonly ConcurrentBag<T> pool = [];
        private readonly Func<T> createNew;
        private readonly Action<T> resetForReuse;

        public SharedPool(Func<T> createNew, Action<T> resetForReuse)
        {
            this.createNew = createNew;
            this.resetForReuse = resetForReuse;
        }

        public T Create()
        {
            if (!this.pool.TryTake(out var result))
            {
                result = this.createNew();
            }

            return result;
        }

        public void Return(T reusable)
        {
            if (this.pool.Count > 10)
            {
                return;
            }

            this.resetForReuse(reusable);
            this.pool.Add(reusable);
        }
    }
}
