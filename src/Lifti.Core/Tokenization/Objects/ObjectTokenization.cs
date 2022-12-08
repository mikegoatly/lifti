using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tokenization.Objects
{

    /// <inheritdoc />
    /// <typeparam name="T">The type of object this tokenization is capable of indexing.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    internal class ObjectTokenization<T, TKey> : IObjectTokenization
    {
        internal ObjectTokenization(
            Func<T, TKey> keyReader,
            IReadOnlyList<FieldReader<T>> fieldReaders)
        {
            this.KeyReader = keyReader;
            this.FieldReaders = fieldReaders.ToDictionary(x => x.Name);
        }

        /// <summary>
        /// Gets the delegate capable of reading the key from the item.
        /// </summary>
        public Func<T, TKey> KeyReader { get; }

        /// <summary>
        /// Gets the set of configurations that determine how fields should be read from an object of 
        /// type <typeparamref name="T"/>.
        /// </summary>
        public IDictionary<string, FieldReader<T>> FieldReaders { get; }

        /// <inheritdoc />
        IEnumerable<IFieldReader> IObjectTokenization.GetConfiguredFields()
        {
            return this.FieldReaders.Values.Cast<IFieldReader>();
        }
    }
}
