using System.Collections.Generic;

namespace Lifti.Serialization
{
    /// <summary>
    /// An abstract base class for collecting deserialized data during a deserialization process.
    /// </summary>
    public abstract class DeserializedDataCollector<T>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DeserializedDataCollector{T}"/> class.
        /// </summary>
        protected DeserializedDataCollector(int expectedCount)
        {
            this.Collected = new List<T>(expectedCount);
        }

        /// <summary>
        /// Adds the metadata to the collection.
        /// </summary>
        public void Add(T item)
        {
            this.Collected.Add(item);
        }

        internal List<T> Collected { get; }
    }
}
