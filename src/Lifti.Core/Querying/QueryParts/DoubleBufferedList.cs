using System;
using System.Collections;
using System.Collections.Generic;

namespace Lifti.Querying.QueryParts
{
    internal class DoubleBufferedList<T> : IEnumerable<T>
    {
        private List<T> current = new List<T>();
        private List<T> swap = new List<T>();

        public DoubleBufferedList()
        {
        }

        public DoubleBufferedList(params T[] initialData)
        {
            this.current.AddRange(initialData);
        }

        public void Add(T item)
        {
            this.swap.Add(item);
        }

        public void AddRange(IEnumerable<T> items)
        {
            this.swap.AddRange(items);
        }

        public int Count => this.current.Count;

        public void Swap()
        {
            var tempStack = this.current;
            tempStack.Clear();
            this.current = this.swap;
            this.swap = tempStack;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.current.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
