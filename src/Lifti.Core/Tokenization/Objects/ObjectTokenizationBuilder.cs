using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading;
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
    public class ObjectTokenizationBuilder<T, TKey>
    {
        private readonly List<FieldReader<T>> fieldReaders = new List<FieldReader<T>>();
        private Func<T, TKey>? keyReader;

        /// <summary>
        /// Indicates how the unique key of the item can be read.
        /// </summary>
        /// <param name="keyReader">
        /// The delegate capable of reading the key from the item
        /// </param>
        public ObjectTokenizationBuilder<T, TKey> WithKey(Func<T, TKey> keyReader)
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
        /// <param name="tokenizationOptions">
        /// An optional delegate capable of building the options that should be used with tokenizing text in this field. If this is 
        /// null then default tokenizer configured for the index will be used.
        /// </param>
        /// <param name="textExtractor">
        /// The <see cref="ITextExtractor"/> to use when indexing text from the field. If this is not specified then the default
        /// text extractor for the index will be used.
        /// </param>
        public ObjectTokenizationBuilder<T, TKey> WithField(
            string name,
            Func<T, string> fieldTextReader,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null)
        {
            ValidateFieldParameters(name, fieldTextReader);
            var tokenizer = tokenizationOptions.CreateTokenizer();
            this.fieldReaders.Add(new StringFieldReader<T>(name, fieldTextReader, tokenizer, textExtractor));
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
        /// <param name="tokenizationOptions">
        /// An optional delegate capable of building the options that should be used with tokenizing text in this field. If this is 
        /// null then default tokenizer configured for the index will be used.
        /// </param>
        /// <param name="textExtractor">
        /// The <see cref="ITextExtractor"/> to use when indexing text from the field. If this is not specified then the default
        /// text extractor for the index will be used.
        /// </param>
        public ObjectTokenizationBuilder<T, TKey> WithField(
            string name,
            Func<T, IEnumerable<string>> reader,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null)
        {
            ValidateFieldParameters(name, reader);
            var tokenizer = tokenizationOptions.CreateTokenizer();
            this.fieldReaders.Add(new StringArrayFieldReader<T>(name, reader, tokenizer, textExtractor));
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
        /// <param name="tokenizationOptions">
        /// An optional delegate capable of building the options that should be used with tokenizing text in this field. If this is 
        /// null then default tokenizer configured for the index will be used.
        /// </param>
        /// <param name="textExtractor">
        /// The <see cref="ITextExtractor"/> to use when indexing text from the field. If this is not specified then the default
        /// text extractor for the index will be used.
        /// </param>
        public ObjectTokenizationBuilder<T, TKey> WithField(
            string name,
            Func<T, CancellationToken, Task<string>> fieldTextReader,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null)
        {
            ValidateFieldParameters(name, fieldTextReader);
            var tokenizer = tokenizationOptions.CreateTokenizer();
            this.fieldReaders.Add(new AsyncStringFieldReader<T>(name, fieldTextReader, tokenizer, textExtractor));
            return this;
        }

        /// <inheritdoc cref="WithField(string, Func{T, CancellationToken, Task{string}}, Func{TokenizerBuilder, TokenizerBuilder}?, ITextExtractor?)"/>
        public ObjectTokenizationBuilder<T, TKey> WithField(
            string name,
            Func<T, Task<string>> fieldTextReader,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null)
        {
            return this.WithField(
                name,
                (item, ctx) => fieldTextReader(item),
                tokenizationOptions,
                textExtractor);
        }

        /// <summary>
        /// Adds a field to be indexed for the item.
        /// </summary>
        /// <param name="name">
        /// The name of the field. This can be referred to when querying to restrict searches to text read for this field only.
        /// </param>
        /// <param name="fieldTextReader">
        /// The delegate capable of reading the entire text for the field, where the text is broken in to multiple fragments.
        /// </param>
        /// <param name="tokenizationOptions">
        /// An optional delegate capable of building the options that should be used with tokenizing text in this field. If this is 
        /// null then default tokenizer configured for the index will be used.
        /// </param>
        /// <param name="textExtractor">
        /// The <see cref="ITextExtractor"/> to use when indexing text from the field. If this is not specified then the default
        /// text extractor for the index will be used.
        /// </param>
        public ObjectTokenizationBuilder<T, TKey> WithField(
            string name,
            Func<T, CancellationToken, Task<IEnumerable<string>>> fieldTextReader,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null)
        {
            ValidateFieldParameters(name, fieldTextReader);
            var tokenizer = tokenizationOptions.CreateTokenizer();
            this.fieldReaders.Add(new AsyncStringArrayFieldReader<T>(name, fieldTextReader, tokenizer, textExtractor));
            return this;
        }

        /// <inheritdoc cref="WithField(string, Func{T, CancellationToken, Task{IEnumerable{string}}}, Func{TokenizerBuilder, TokenizerBuilder}?, ITextExtractor?)" />
        public ObjectTokenizationBuilder<T, TKey> WithField(
            string name,
            Func<T, Task<IEnumerable<string>>> fieldTextReader,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null)
        {
            return this.WithField(
                name,
                (item, ctx) => fieldTextReader(item),
                tokenizationOptions,
                textExtractor);
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
        internal ObjectTokenization<T, TKey> Build()
        {
            if (this.keyReader == null)
            {
                throw new LiftiException(ExceptionMessages.KeyReaderMustBeProvided);
            }

            if (this.fieldReaders.Count == 0)
            {
                throw new LiftiException(ExceptionMessages.AtLeastOneFieldMustBeIndexed);
            }

            return new ObjectTokenization<T, TKey>(
                this.keyReader,
                this.fieldReaders);
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
