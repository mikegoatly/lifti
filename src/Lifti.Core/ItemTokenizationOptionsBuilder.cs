using System;
using System.Collections.Generic;

namespace Lifti
{
    public class ItemTokenizationOptionsBuilder<TItem, TKey>
    {
        private List<FieldTokenizationOptions<TItem>> fieldTokenization { get; } = new List<FieldTokenizationOptions<TItem>>();
        private Func<TItem, TKey> keyReader;

        /// <summary>
        /// Indicates how the unique key of the item can be read.
        /// </summary>
        /// <param name="keyReader">
        /// The delegate capable of reading the key from the item
        /// </param>
        public ItemTokenizationOptionsBuilder<TItem, TKey> WithKey(Func<TItem, TKey> keyReader)
        {
            if (keyReader is null)
            {
                throw new ArgumentNullException(nameof(keyReader));
            }

            this.keyReader = keyReader;

            return this;
        }

        /// <summary>
        /// Adds a field to be indexed for the item.
        /// </summary>
        /// <param name="name">
        /// The name of the field. This can be referred to when querying to restrict searches to text read for this field only.
        /// </param>
        /// <param name="reader">
        /// The delegate capable of reading the entire text for the field.
        /// </param>
        /// <param name="optionsBuilder">
        /// An optional delegate capable of building the options that should be used with tokenizing text in this field. If this is 
        /// null then <see cref="TokenizationOptions.Default"/> will be used when processing text - this provides a tokenizer that
        /// is accent and case insensitive that splits on punctuation and whitespace, but does <b>not</b> perform word stemming.
        /// </param>
        public ItemTokenizationOptionsBuilder<TItem, TKey> WithField(
            string name, 
            Func<TItem, string> reader, 
            Func<TokenizationOptionsBuilder, TokenizationOptionsBuilder> optionsBuilder = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(ExceptionMessages.FieldNameMustNotBeEmpty, nameof(name));
            }

            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var tokenizationOptions = optionsBuilder.BuildOptionsOrDefault();
            this.fieldTokenization.Add(new FieldTokenizationOptions<TItem>(name, reader, tokenizationOptions));
            return this;
        }

        /// <summary>
        /// Adds a field to be indexed for the item.
        /// </summary>
        /// <param name="name">
        /// The name of the field. This can be referred to when querying to restrict searches to text read for this field only.
        /// </param>
        /// <param name="reader">
        /// The delegate capable of reading the entire text for the field.
        /// </param>
        /// <param name="optionsBuilder">
        /// An optional delegate capable of building the options that should be used with tokenizing text in this field. If this is 
        /// null then <see cref="TokenizationOptions.Default"/> will be used when processing text - this provides a tokenizer that
        /// is accent and case insensitive that splits on punctuation and whitespace, but does <b>not</b> perform word stemming.
        /// </param>
        public ItemTokenizationOptionsBuilder<TItem, TKey> WithField(
            string name,
            Func<TItem, IEnumerable<string>> reader,
            Func<TokenizationOptionsBuilder, TokenizationOptionsBuilder> optionsBuilder = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(ExceptionMessages.FieldNameMustNotBeEmpty, nameof(name));
            }

            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var tokenizationOptions = optionsBuilder.BuildOptionsOrDefault();
            this.fieldTokenization.Add(new FieldTokenizationOptions<TItem>(name, reader, tokenizationOptions));
            return this;
        }

        public ItemTokenizationOptions<TItem, TKey> Build()
        {
            if (this.keyReader == null)
            {
                throw new LiftiException(ExceptionMessages.KeyReaderMustBeProvided);
            }

            if (this.fieldTokenization.Count == 0)
            {
                throw new LiftiException(ExceptionMessages.AtLeastOneFieldMustBeIndexed);
            }

            return new ItemTokenizationOptions<TItem, TKey>(
                this.keyReader,
                this.fieldTokenization);
        }
    }
}
