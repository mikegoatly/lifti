using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    internal interface IObjectTokenizationBuilder
    {
        /// <summary>
        /// Builds this instance.
        /// </summary>
        /// <param name="defaultTokenizer">The default <see cref="IIndexTokenizer"/> to use when one is not 
        /// explicitly configured for a field.</param>
        /// <param name="defaultThesaurusBuilder">The default <see cref="ThesaurusBuilder"/>
        /// to use when one is not explicitly configured for a field.</param>
        /// <param name="defaultTextExtractor">The default <see cref="ITextExtractor"/> to use when one is not 
        /// explicitly configured for a field.</param>
        /// <exception cref="LiftiException">
        /// Thrown if:
        /// * <see cref="ObjectTokenizationBuilder{T, TKey}.WithKey(Func{T, TKey})"/> has not been called.
        /// * No fields have been configured.
        /// </exception>
        IObjectTokenization Build(IIndexTokenizer defaultTokenizer, ThesaurusBuilder defaultThesaurusBuilder, ITextExtractor defaultTextExtractor);
    }

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
    public class ObjectTokenizationBuilder<T, TKey> : IObjectTokenizationBuilder
    {
        private readonly List<Func<IIndexTokenizer, ThesaurusBuilder, ITextExtractor, FieldReader<T>>> fieldReaderBuilders = new();
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
        /// <param name="thesaurusOptions">
        /// An optional delefate capable of building the thesaurus for this field. If this is unspecified then the default thesaurus
        /// for the index will be used.
        /// </param>
        /// <param name="textExtractor">
        /// The <see cref="ITextExtractor"/> to use when indexing text from the field. If this is not specified then the default
        /// text extractor for the index will be used.
        /// </param>
        public ObjectTokenizationBuilder<T, TKey> WithField(
            string name,
            Func<T, string> fieldTextReader,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null,
            Func<ThesaurusBuilder, ThesaurusBuilder>? thesaurusOptions = null)
        {
            ValidateFieldParameters(name, fieldTextReader);
            var tokenizer = tokenizationOptions.CreateTokenizer();
            this.fieldReaderBuilders.Add(
                (defaultTokenizer, defaultThesaurusBuilder, defaultTextExtractor) => new StringFieldReader<T>(
                    name,
                    fieldTextReader,
                    tokenizer ?? defaultTokenizer,
                    textExtractor ?? defaultTextExtractor,
                    CreateFieldThesaurus(defaultTokenizer, tokenizer, defaultThesaurusBuilder, thesaurusOptions)));

            return this;
        }

        private static IThesaurus CreateFieldThesaurus(
            IIndexTokenizer defaultTokenizer,
            IIndexTokenizer? fieldTokenizer,
            ThesaurusBuilder defaultThesaurusBuilder,
            Func<ThesaurusBuilder, ThesaurusBuilder>? thesaurusOptions)
        {
            var tokenizer = fieldTokenizer ?? defaultTokenizer;
            if (thesaurusOptions == null)
            {
                // Use the default thesaurus. Note we need to build it again in case the tokenization is configured differently for this field.
                return defaultThesaurusBuilder.Build(tokenizer);
            }

            return thesaurusOptions(new ThesaurusBuilder()).Build(tokenizer);
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
        /// <param name="thesaurusOptions">
        /// An optional delefate capable of building the thesaurus for this field. If this is unspecified then the default thesaurus
        /// for the index will be used.
        /// </param>
        public ObjectTokenizationBuilder<T, TKey> WithField(
            string name,
            Func<T, IEnumerable<string>> reader,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null,
            Func<ThesaurusBuilder, ThesaurusBuilder>? thesaurusOptions = null)
        {
            ValidateFieldParameters(name, reader);
            var tokenizer = tokenizationOptions.CreateTokenizer();
            this.fieldReaderBuilders.Add((defaultTokenizer, defaultThesaurusBuilder, defaultTextExtractor) =>
                new StringArrayFieldReader<T>(
                    name,
                    reader,
                    tokenizer ?? defaultTokenizer,
                    textExtractor ?? defaultTextExtractor,
                    CreateFieldThesaurus(defaultTokenizer, tokenizer, defaultThesaurusBuilder, thesaurusOptions)));

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
        /// <param name="thesaurusOptions">
        /// An optional delefate capable of building the thesaurus for this field. If this is unspecified then the default thesaurus
        /// for the index will be used.
        /// </param>
        public ObjectTokenizationBuilder<T, TKey> WithField(
            string name,
            Func<T, CancellationToken, Task<string>> fieldTextReader,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null,
            Func<ThesaurusBuilder, ThesaurusBuilder>? thesaurusOptions = null)
        {
            ValidateFieldParameters(name, fieldTextReader);
            var tokenizer = tokenizationOptions.CreateTokenizer();
            this.fieldReaderBuilders.Add((defaultTokenizer, defaultThesaurusBuilder, defaultTextExtractor) =>
                new AsyncStringFieldReader<T>(
                    name,
                    fieldTextReader,
                    tokenizer ?? defaultTokenizer,
                    textExtractor ?? defaultTextExtractor,
                    CreateFieldThesaurus(defaultTokenizer, tokenizer, defaultThesaurusBuilder, thesaurusOptions)));

            return this;
        }

        /// <inheritdoc cref="WithField(string, Func{T, CancellationToken, Task{string}}, Func{TokenizerBuilder, TokenizerBuilder}?, ITextExtractor?, Func{ThesaurusBuilder, ThesaurusBuilder}?)"/>
        public ObjectTokenizationBuilder<T, TKey> WithField(
            string name,
            Func<T, Task<string>> fieldTextReader,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null, 
            Func<ThesaurusBuilder, ThesaurusBuilder>? thesaurusOptions = null)
        {
            return this.WithField(
                name,
                (item, ctx) => fieldTextReader(item),
                tokenizationOptions,
                textExtractor,
                thesaurusOptions);
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
        /// <param name="thesaurusOptions">
        /// An optional delefate capable of building the thesaurus for this field. If this is unspecified then the default thesaurus
        /// for the index will be used.
        /// </param>
        public ObjectTokenizationBuilder<T, TKey> WithField(
            string name,
            Func<T, CancellationToken, Task<IEnumerable<string>>> fieldTextReader,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null,
            Func<ThesaurusBuilder, ThesaurusBuilder>? thesaurusOptions = null)
        {
            ValidateFieldParameters(name, fieldTextReader);
            var tokenizer = tokenizationOptions.CreateTokenizer();
            this.fieldReaderBuilders.Add((defaultTokenizer, defaultThesaurusBuilder, defaultTextExtractor) =>
                new AsyncStringArrayFieldReader<T>(
                    name,
                    fieldTextReader,
                    tokenizer ?? defaultTokenizer,
                    textExtractor ?? defaultTextExtractor,
                    CreateFieldThesaurus(defaultTokenizer, tokenizer, defaultThesaurusBuilder, thesaurusOptions)));

            return this;
        }

        /// <inheritdoc cref="WithField(string, Func{T, CancellationToken, Task{IEnumerable{string}}}, Func{TokenizerBuilder, TokenizerBuilder}?, ITextExtractor?, Func{ThesaurusBuilder, ThesaurusBuilder}?)" />
        public ObjectTokenizationBuilder<T, TKey> WithField(
            string name,
            Func<T, Task<IEnumerable<string>>> fieldTextReader,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            Func<ThesaurusBuilder, ThesaurusBuilder>? thesaurusOptions = null,
            ITextExtractor? textExtractor = null)
        {
            return this.WithField(
                name,
                (item, ctx) => fieldTextReader(item),
                tokenizationOptions,
                textExtractor,
                thesaurusOptions);
        }

        /// <inheritdoc />
        IObjectTokenization IObjectTokenizationBuilder.Build(IIndexTokenizer defaultTokenizer, ThesaurusBuilder defaultThesaurusBuilder, ITextExtractor defaultTextExtractor)
        {
            if (this.keyReader == null)
            {
                throw new LiftiException(ExceptionMessages.KeyReaderMustBeProvided);
            }

            if (this.fieldReaderBuilders.Count == 0)
            {
                throw new LiftiException(ExceptionMessages.AtLeastOneFieldMustBeIndexed);
            }

            return new ObjectTokenization<T, TKey>(
                this.keyReader,
                this.fieldReaderBuilders.Select(builder => builder(defaultTokenizer, defaultThesaurusBuilder, defaultTextExtractor)).ToList());
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
