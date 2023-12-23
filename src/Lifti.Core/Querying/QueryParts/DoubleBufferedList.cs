using System.Collections;
using System.Collections.Generic;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// A list that allows for mutations to take place on a separate list to the one being enumerated, and then swapped.
    /// </summary>
    internal class DoubleBufferedList<T> : IEnumerable<T>
    {
        private List<T> current = [];
        private List<T> swap = [];

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
