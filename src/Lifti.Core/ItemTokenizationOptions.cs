using System;
using System.Collections.Generic;

namespace Lifti
{
    public class ItemTokenizationOptions<TItem, TKey>
    {
        private List<FieldTokenizationOptions<TItem>> fieldTokenization = new List<FieldTokenizationOptions<TItem>>();

        internal ItemTokenizationOptions(
            Func<TItem, TKey> idReader)
        {
            this.KeyReader = idReader;
        }

        /// <summary>
        /// Gets the delegate capable of reading the key from the item.
        /// </summary>
        public Func<TItem, TKey> KeyReader { get; }

        /// <summary>
        /// Gets the set of configurations that determine how fields should be read from an object of 
        /// type <see cref="TItem"/>.
        /// </summary>
        public IReadOnlyList<FieldTokenizationOptions<TItem>> FieldTokenization => this.fieldTokenization;

        /// <summary>
        /// Adds a field to be indexed for the item.
        /// </summary>
        /// <param name="name">
        /// The name of the field. This can be referred to when querying to restrict searches to text read for this field only.
        /// </param>
        /// <param name="reader">
        /// The delegate capable of reading the entire text for the field.
        /// </param>
        /// <param name="tokenizationOptions">
        /// The tokenization options to be used when reading tokens for this field
        /// </param>
        public ItemTokenizationOptions<TItem, TKey> WithField(string name, Func<TItem, string> reader, TokenizationOptions tokenizationOptions = null)
        {
            this.fieldTokenization.Add(new FieldTokenizationOptions<TItem>(name, reader, tokenizationOptions));
            return this;
        }
    }
}
