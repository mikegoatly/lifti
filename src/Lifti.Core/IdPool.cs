using System;
using System.Collections.Generic;

namespace Lifti
{
    /// <summary>
    /// Provides methods for generating unique internal ids for items based on the key of the item.
    /// </summary>
    /// <typeparam name="TKey">The type of key in the index.</typeparam>
    internal class IdPool<TKey>
        where TKey : notnull
    {
        private readonly Queue<int> reusableIds = new();
        private int nextId;

        /// <summary>
        /// Gets the next available id from the pool.
        /// </summary>
        public int Next()
        {
            return this.reusableIds.Count == 0 ? this.nextId++ : this.reusableIds.Dequeue();
        }

        /// <summary>
        /// Returns the given id to the pool.
        /// </summary>
        public void Return(int id)
        {
            this.reusableIds.Enqueue(id);
        }

        /// <summary>
        /// Used during index deserialization to ensure that the next id generated is greater than any id used in 
        /// the index.
        /// </summary>
        internal void RegisterUsedId(int id)
        {
            this.nextId = Math.Max(this.nextId, id + 1);
        }
    }
}
