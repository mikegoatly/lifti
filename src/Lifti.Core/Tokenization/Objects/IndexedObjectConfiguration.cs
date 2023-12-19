using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tokenization.Objects
{

    /// <inheritdoc />
    /// <typeparam name="TObject">The type of object this tokenization is capable of indexing.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    internal class IndexedObjectConfiguration<TObject, TKey> : IIndexedObjectConfiguration
    {
        internal IndexedObjectConfiguration(
            byte id,
            Func<TObject, TKey> keyReader,
            IReadOnlyList<StaticFieldReader<TObject>> fieldReaders,
            IReadOnlyList<DynamicFieldReader<TObject>> dynamicFieldReaders,
            ObjectScoreBoostOptions<TObject> scoreBoostOptions)
        {
            this.Id = id;
            this.KeyReader = keyReader;
            this.FieldReaders = fieldReaders.ToDictionary(x => x.Name);
            this.DynamicFieldReaders = dynamicFieldReaders;
            this.ScoreBoostOptions = scoreBoostOptions;
        }

        /// <inheritdoc />
        public byte Id { get; }

        /// <summary>
        /// Gets the delegate capable of reading the key from the item.
        /// </summary>
        public Func<TObject, TKey> KeyReader { get; }

        /// <summary>
        /// Gets the set of configurations for fields that can be defined statically at index creation.
        /// </summary>
        public IDictionary<string, StaticFieldReader<TObject>> FieldReaders { get; }

        /// <summary>
        /// Gets the set of configurations that determine dynamic fields that can only be known during indexing.
        /// </summary>
        public IReadOnlyList<DynamicFieldReader<TObject>> DynamicFieldReaders { get; }

        /// <summary>
        /// The score boost options for the object type.
        /// </summary>
        public ObjectScoreBoostOptions<TObject> ScoreBoostOptions { get; }

        /// <inheritdoc />
        Type IIndexedObjectConfiguration.ItemType { get; } = typeof(TObject);

        /// <inheritdoc />
        ObjectScoreBoostOptions IIndexedObjectConfiguration.ScoreBoostOptions => this.ScoreBoostOptions;
    }
}
