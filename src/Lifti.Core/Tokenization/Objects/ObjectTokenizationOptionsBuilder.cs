using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    /// <summary>
    /// The builder class used to configure an object type for indexing. The object type <typeparamref name="T"/>
    /// must expose an id property of type <typeparamref name="TKey"/> configured using the <see cref="WithKey(Func{T, TKey})"/>
    /// method.
    /// </summary>
    /// <typeparam name="T">
    /// The type of object to configure.
    /// </typeparam>
    /// <typeparam name="TKey">
    /// The type of key in the index.
    /// </typeparam>
    public class ObjectTokenizationOptionsBuilder<T, TKey>
    {
        private List<FieldTokenization<T>> fieldTokenization { get; } = new List<FieldTokenization<T>>();
        private Func<T, TKey>? keyReader;

        /// <summary>
        /// Indicates how the unique key of the item can be read.
        /// </summary>
        /// <param name="keyReader">
        /// The delegate capable of reading the key from the item
        /// </param>
        public ObjectTokenizationOptionsBuilder<T, TKey> WithKey(Func<T, TKey> keyReader)
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
        /// <param name="fieldTextReader">
        /// The delegate capable of reading the entire text for the field.
        /// </param>
        /// <param name="optionsBuilder">
        /// An optional delegate capable of building the options that should be used with tokenizing text in this field. If this is 
        /// null then <see cref="TokenizationOptions.Default"/> will be used when processing text - this provides a tokenizer that
        /// is accent and case insensitive that splits on punctuation and whitespace, but does <b>not</b> perform word stemming.
        /// </param>
        public ObjectTokenizationOptionsBuilder<T, TKey> WithField(
            string name,
            Func<T, string> fieldTextReader,
            Func<TokenizationOptionsBuilder, TokenizationOptionsBuilder>? optionsBuilder = null)
        {
            ValidateFieldParameters(name, fieldTextReader);
            var tokenizationOptions = optionsBuilder == null ? null : optionsBuilder.BuildOptionsOrDefault();
            this.fieldTokenization.Add(new StringReaderFieldTokenizationOptions<T>(name, fieldTextReader, tokenizationOptions));
            return this;
        }

        /// <summary>
        /// Adds a field to be indexed for the item.
        /// </summary>
        /// <param name="name">
        /// The name of the field. This can be referred to when querying to restrict searches to text read for this field only.
        /// </param>
        /// <param name="reader">
        /// The delegate capable of reading the entire text for the field, where the text is broken in to multiple fragments.
        /// </param>
        /// <param name="optionsBuilder">
        /// An optional delegate capable of building the options that should be used with tokenizing text in this field. If this is 
        /// null then <see cref="TokenizationOptions.Default"/> will be used when processing text - this provides a tokenizer that
        /// is accent and case insensitive that splits on punctuation and whitespace, but does <b>not</b> perform word stemming.
        /// </param>
        public ObjectTokenizationOptionsBuilder<T, TKey> WithField(
            string name,
            Func<T, IEnumerable<string>> reader,
            Func<TokenizationOptionsBuilder, TokenizationOptionsBuilder>? optionsBuilder = null)
        {
            ValidateFieldParameters(name, reader);
            var tokenizationOptions = optionsBuilder == null ? null : optionsBuilder.BuildOptionsOrDefault();
            this.fieldTokenization.Add(new StringArrayReaderFieldTokenizationOptions<T>(name, reader, tokenizationOptions));
            return this;
        }

        /// <summary>
        /// Adds a field to be indexed for the item.
        /// </summary>
        /// <param name="name">
        /// The name of the field. This can be referred to when querying to restrict searches to text read for this field only.
        /// </param>
        /// <param name="fieldTextReader">
        /// The delegate capable of reading the entire text for the field.
        /// </param>
        /// <param name="optionsBuilder">
        /// An optional delegate capable of building the options that should be used with tokenizing text in this field. If this is 
        /// null then <see cref="TokenizationOptions.Default"/> will be used when processing text - this provides a tokenizer that
        /// is accent and case insensitive that splits on punctuation and whitespace, but does <b>not</b> perform word stemming.
        /// </param>
        public ObjectTokenizationOptionsBuilder<T, TKey> WithField(
            string name,
            Func<T, Task<string>> fieldTextReader,
            Func<TokenizationOptionsBuilder, TokenizationOptionsBuilder>? optionsBuilder = null)
        {
            ValidateFieldParameters(name, fieldTextReader);
            var tokenizationOptions = optionsBuilder == null ? null : optionsBuilder.BuildOptionsOrDefault();
            this.fieldTokenization.Add(new AsyncStringReaderFieldTokenizationOptions<T>(name, fieldTextReader, tokenizationOptions));
            return this;
        }

        /// <summary>
        /// Adds a field to be indexed for the item.
        /// </summary>
        /// <param name="name">
        /// The name of the field. This can be referred to when querying to restrict searches to text read for this field only.
        /// </param>
        /// <param name="reader">
        /// The delegate capable of reading the entire text for the field, where the text is broken in to multiple fragments.
        /// </param>
        /// <param name="optionsBuilder">
        /// An optional delegate capable of building the options that should be used with tokenizing text in this field. If this is 
        /// null then <see cref="TokenizationOptions.Default"/> will be used when processing text - this provides a tokenizer that
        /// is accent and case insensitive that splits on punctuation and whitespace, but does <b>not</b> perform word stemming.
        /// </param>
        public ObjectTokenizationOptionsBuilder<T, TKey> WithField(
            string name,
            Func<T, Task<IEnumerable<string>>> reader,
            Func<TokenizationOptionsBuilder, TokenizationOptionsBuilder>? optionsBuilder = null)
        {
            ValidateFieldParameters(name, reader);
            var tokenizationOptions = optionsBuilder == null ? null : optionsBuilder.BuildOptionsOrDefault();
            this.fieldTokenization.Add(new AsyncStringArrayReaderFieldTokenizationOptions<T>(name, reader, tokenizationOptions));
            return this;
        }

        /// <summary>
        /// Builds this instance.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="LiftiException">
        /// Thrown if:
        /// * <see cref="WithKey(Func{T, TKey})"/> has not been called.
        /// * No fields have been configured.
        /// </exception>
        public ObjectTokenization<T, TKey> Build()
        {
            if (this.keyReader == null)
            {
                throw new LiftiException(ExceptionMessages.KeyReaderMustBeProvided);
            }

            if (this.fieldTokenization.Count == 0)
            {
                throw new LiftiException(ExceptionMessages.AtLeastOneFieldMustBeIndexed);
            }

            return new ObjectTokenization<T, TKey>(
                this.keyReader,
                this.fieldTokenization);
        }

        private static void ValidateFieldParameters(string name, object fieldTextReader)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(ExceptionMessages.FieldNameMustNotBeEmpty, nameof(name));
            }

            if (fieldTextReader is null)
            {
                throw new ArgumentNullException(nameof(fieldTextReader));
            }
        }
    }
}
