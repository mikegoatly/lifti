using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tokenization.Objects
{

    /// <inheritdoc />
    /// <typeparam name="T">The type of object this tokenization is capable of indexing.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    internal class IndexedObjectConfiguration<T, TKey> : IIndexedObjectConfiguration
    {
        internal IndexedObjectConfiguration(
            Func<T, TKey> keyReader,
            IReadOnlyList<StaticFieldReader<T>> fieldReaders,
            IReadOnlyList<DynamicFieldReader<T>> dynamicFieldReaders,
            ObjectScoreBoostOptions<T> scoreBoostOptions)
        {
            this.KeyReader = keyReader;
            this.FieldReaders = fieldReaders.ToDictionary(x => x.Name);
            this.DynamicFieldReaders = dynamicFieldReaders;
            this.ScoreBoostOptions = scoreBoostOptions;
        }

        /// <summary>
        /// Gets the delegate capable of reading the key from the item.
        /// </summary>
        public Func<T, TKey> KeyReader { get; }

        /// <summary>
        /// Gets the set of configurations for fields that can be defined statically at index creation.
        /// </summary>
        public IDictionary<string, StaticFieldReader<T>> FieldReaders { get; }

        /// <summary>
        /// Gets the set of configurations that determine dynamic fields that can only be known during indexing.
        /// </summary>
        public IReadOnlyList<DynamicFieldReader<T>> DynamicFieldReaders { get; }

        /// <summary>
        /// The score boost options for the object type.
        /// </summary>
        public ObjectScoreBoostOptions<T> ScoreBoostOptions { get; }

        /// <inheritdoc />
        Type IIndexedObjectConfiguration.ItemType { get; } = typeof(T);
    }
}
