using System;
using System.Collections.Generic;

namespace Lifti.Tokenization.Objects
{
    /// <inheritdoc/>
    public class ItemTokenization<TItem, TKey> : IItemTokenization
    {
        internal ItemTokenization(
            Func<TItem, TKey> keyReader,
            IReadOnlyList<FieldTokenization<TItem>> fieldTokenizationOptions)
        {
            this.KeyReader = keyReader;
            this.FieldTokenization = fieldTokenizationOptions;
        }

        /// <summary>
        /// Gets the delegate capable of reading the key from the item.
        /// </summary>
        public Func<TItem, TKey> KeyReader { get; }

        /// <summary>
        /// Gets the set of configurations that determine how fields should be read from an object of 
        /// type <typeparamref name="TItem"/>.
        /// </summary>
        public IReadOnlyList<FieldTokenization<TItem>> FieldTokenization { get; }

        /// <inheritdoc />
        IEnumerable<IFieldTokenization> IItemTokenization.GetConfiguredFields()
        {
            return this.FieldTokenization;
        }
    }
}
