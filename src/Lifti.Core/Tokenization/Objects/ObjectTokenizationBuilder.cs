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
        /// <param name="objectTypeId">
        /// The unique id for the object type.
        /// </param>
        /// <param name="defaultTokenizer">The default <see cref="IIndexTokenizer"/> to use when one is not 
        /// explicitly configured for a field.</param>
        /// <param name="defaultThesaurusBuilder">The default <see cref="ThesaurusBuilder"/>
        /// to use when one is not explicitly configured for a field.</param>
        /// <param name="defaultTextExtractor">The default <see cref="ITextExtractor"/> to use when one is not 
        /// explicitly configured for a field.</param>
        /// <param name="fieldLookup">
        /// The field lookup with which to register any static fields.
        /// </param>
        /// <exception cref="LiftiException">
        /// Thrown if:
        /// * <see cref="ObjectTokenizationBuilder{T, TKey}.WithKey(Func{T, TKey})"/> has not been called.
        /// * No fields have been configured.
        /// </exception>
        IObjectTypeConfiguration Build(
            byte objectTypeId,
            IIndexTokenizer defaultTokenizer,
            ThesaurusBuilder defaultThesaurusBuilder,
            ITextExtractor defaultTextExtractor,
            IndexedFieldLookup fieldLookup);
    }

    /// <summary>
    /// The builder class used to configure an object type for indexing. The object type <typeparamref name="TObject"/>
    /// must expose an id property of type <typeparamref name="TKey"/> configured using the <see cref="WithKey(Func{TObject, TKey})"/>
    /// method.
    /// </summary>
    /// <typeparam name="TObject">
    /// The type of object to configure.
    /// </typeparam>
    /// <typeparam name="TKey">
    /// The type of key in the index.
    /// </typeparam>
    public class ObjectTokenizationBuilder<TObject, TKey> : IObjectTokenizationBuilder
    {
        private readonly List<Func<IIndexTokenizer, ThesaurusBuilder, ITextExtractor, StaticFieldReader<TObject>>> fieldReaderBuilders = [];
        private Func<TObject, TKey>? keyReader;
        private readonly List<Func<IIndexTokenizer, ThesaurusBuilder, ITextExtractor, DynamicFieldReader<TObject>>> dynamicFieldReaderBuilders = [];
        private ObjectScoreBoostBuilder<TObject>? objectScoreBoostBuilder;

        /// <summary>
        /// Indicates how the unique key of the object can be read.
        /// </summary>
        /// <param name="keyReader">
        /// The delegate capable of reading the key from the object.
        /// </param>
        public ObjectTokenizationBuilder<TObject, TKey> WithKey(Func<TObject, TKey> keyReader)
        {
            if (keyReader is null)
            {
                throw new ArgumentNullException(nameof(keyReader));
            }

            this.keyReader = keyReader;

            return this;
        }

        /// <summary>
        /// Adds a field to be indexed for the object.
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
        /// An optional delegate capable of building the thesaurus for this field. If this is unspecified then the default thesaurus
        /// for the index will be used.
        /// </param>
        /// <param name="textExtractor">
        /// The <see cref="ITextExtractor"/> to use when indexing text from the field. If this is not specified then the default
        /// text extractor for the index will be used.
        /// </param>
        /// <param name="scoreBoost">
        /// The multiplier to apply to the score of this field when ranking results. The default value of 1 is equivalent to no boosting.
        /// </param>
        public ObjectTokenizationBuilder<TObject, TKey> WithField(
            string name,
            Func<TObject, string> fieldTextReader,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null,
            Func<ThesaurusBuilder, ThesaurusBuilder>? thesaurusOptions = null,
            double scoreBoost = 1D)
        {
            ValidateFieldParameters(name, fieldTextReader);
            var tokenizer = tokenizationOptions.CreateTokenizer();
            this.fieldReaderBuilders.Add(
                (defaultTokenizer, defaultThesaurusBuilder, defaultTextExtractor) => new StringFieldReader<TObject>(
                    name,
                    fieldTextReader,
                    tokenizer ?? defaultTokenizer,
                    textExtractor ?? defaultTextExtractor,
                    CreateFieldThesaurus(defaultTokenizer, tokenizer, defaultThesaurusBuilder, thesaurusOptions),
                    scoreBoost));

            return this;
        }

        /// <summary>
        /// Registers a property for the object that exposes a set of dynamic fields and the text to be indexed for each.
        /// Dynamic fields are automatically registered with the index's <see cref="IndexedFieldLookup"/> as they are encountered
        /// during indexing.
        /// </summary>
        /// <param name="dynamicFieldReaderName">
        /// The unique name for this dynamic field reader. This is used when deserializing an index and
        /// restoring the relationship between the a dynamic field and its source provider.
        /// </param>
        /// <param name="dynamicFieldReader">
        /// The delegate capable of reading the the field name/text pairs from the object.
        /// </param>
        /// <param name="fieldNamePrefix">
        /// The optional prefix to apply to any field names read using the <paramref name="dynamicFieldReader"/>.
        /// Use this if you need to register multiple sets of dynamic fields for the same object and there is a 
        /// chance the field names will overlap.
        /// </param>
        /// <param name="tokenizationOptions">
        /// An optional delegate capable of building the options that should be used with tokenizing text in this field. If this is 
        /// null then default tokenizer configured for the index will be used.
        /// </param>
        /// <param name="thesaurusOptions">
        /// An optional delegate capable of building the thesaurus for this field. If this is unspecified then the default thesaurus
        /// for the index will be used.
        /// </param>
        /// <param name="textExtractor">
        /// The <see cref="ITextExtractor"/> to use when indexing text from the field. If this is not specified then the default
        /// text extractor for the index will be used.
        /// </param>
        /// <param name="scoreBoost">
        /// The multiplier to apply to the score of this field when ranking results. The default value of 1 is equivalent to no boosting.
        /// </param>
        public ObjectTokenizationBuilder<TObject, TKey> WithDynamicFields(
            string dynamicFieldReaderName,
            Func<TObject, IDictionary<string, string>?> dynamicFieldReader,
            string? fieldNamePrefix = null,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null,
            Func<ThesaurusBuilder, ThesaurusBuilder>? thesaurusOptions = null,
            double scoreBoost = 1D)
        {
            if (dynamicFieldReader == null)
            {
                throw new ArgumentNullException(nameof(dynamicFieldReader));
            }

            var tokenizer = tokenizationOptions.CreateTokenizer();
            this.dynamicFieldReaderBuilders.Add(
                (defaultTokenizer, defaultThesaurusBuilder, defaultTextExtractor) => new StringDictionaryDynamicFieldReader<TObject>(
                    dynamicFieldReader,
                    dynamicFieldReaderName,
                    fieldNamePrefix,
                    tokenizer ?? defaultTokenizer,
                    textExtractor ?? defaultTextExtractor,
                    CreateFieldThesaurus(defaultTokenizer, tokenizer, defaultThesaurusBuilder, thesaurusOptions),
                    scoreBoost));

            return this;
        }

        /// <inheritdoc cref="WithDynamicFields(string, Func{TObject, IDictionary{string, string}?}, string?, Func{TokenizerBuilder, TokenizerBuilder}?, ITextExtractor?, Func{ThesaurusBuilder, ThesaurusBuilder}?, double)"/>
        public ObjectTokenizationBuilder<TObject, TKey> WithDynamicFields(
            string dynamicFieldReaderName,
            Func<TObject, IDictionary<string, IEnumerable<string>>> dynamicFieldReader,
            string? fieldNamePrefix = null,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null,
            Func<ThesaurusBuilder, ThesaurusBuilder>? thesaurusOptions = null,
            double scoreBoost = 1D)
        {
            if (dynamicFieldReader == null)
            {
                throw new ArgumentNullException(nameof(dynamicFieldReader));
            }

            var tokenizer = tokenizationOptions.CreateTokenizer();
            this.dynamicFieldReaderBuilders.Add(
                (defaultTokenizer, defaultThesaurusBuilder, defaultTextExtractor) => new StringArrayDictionaryDynamicFieldReader<TObject>(
                    dynamicFieldReaderName,
                    dynamicFieldReader,
                    fieldNamePrefix,
                    tokenizer ?? defaultTokenizer,
                    textExtractor ?? defaultTextExtractor,
                    CreateFieldThesaurus(defaultTokenizer, tokenizer, defaultThesaurusBuilder, thesaurusOptions),
                    scoreBoost));

            return this;
        }

        /// <summary>
        /// Registers a property for the object that exposes a set of dynamic fields and the text to be indexed for each.
        /// Dynamic fields are automatically registered with the index's <see cref="IndexedFieldLookup"/> as they are encountered
        /// during indexing.
        /// </summary>
        /// <param name="dynamicFieldReaderName">
        /// The unique name for this dynamic field reader. This is used when deserializing an index and
        /// restoring the relationship between the a dynamic field and its source provider.
        /// </param>
        /// <param name="dynamicFieldReader">
        /// The delegate capable of reading the child objects.
        /// </param>
        /// <param name="getFieldName">
        /// The delegate capable of reading the field name from a child object.
        /// </param>
        /// <param name="getFieldText">
        /// The delegate capable of reading the text to be indexed from a child object.
        /// </param>
        /// <param name="fieldNamePrefix">
        /// The optional prefix to apply to any field names read using the <paramref name="dynamicFieldReader"/>.
        /// Use this if you need to register multiple sets of dynamic fields for the same object and there is a 
        /// chance the field names will overlap.
        /// </param>
        /// <param name="tokenizationOptions">
        /// An optional delegate capable of building the options that should be used with tokenizing text in this field. If this is 
        /// null then default tokenizer configured for the index will be used.
        /// </param>
        /// <param name="thesaurusOptions">
        /// An optional delegate capable of building the thesaurus for this field. If this is unspecified then the default thesaurus
        /// for the index will be used.
        /// </param>
        /// <param name="textExtractor">
        /// The <see cref="ITextExtractor"/> to use when indexing text from the field. If this is not specified then the default
        /// text extractor for the index will be used.
        /// </param>
        /// <param name="scoreBoost">
        /// The multiplier to apply to the score of this field when ranking results. The default value of 1 is equivalent to no boosting.
        /// </param>
        public ObjectTokenizationBuilder<TObject, TKey> WithDynamicFields<TChild>(
            string dynamicFieldReaderName,
            Func<TObject, ICollection<TChild>?> dynamicFieldReader,
            Func<TChild, string> getFieldName,
            Func<TChild, string> getFieldText,
            string? fieldNamePrefix = null,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null,
            Func<ThesaurusBuilder, ThesaurusBuilder>? thesaurusOptions = null,
            double scoreBoost = 1D)
        {
            if (dynamicFieldReader == null)
            {
                throw new ArgumentNullException(nameof(dynamicFieldReader));
            }

            var tokenizer = tokenizationOptions.CreateTokenizer();
            this.dynamicFieldReaderBuilders.Add(
                (defaultTokenizer, defaultThesaurusBuilder, defaultTextExtractor) => new StringChildObjectDynamicFieldReader<TObject, TChild>(
                    dynamicFieldReader,
                    getFieldName,
                    getFieldText,
                    dynamicFieldReaderName,
                    fieldNamePrefix,
                    tokenizer ?? defaultTokenizer,
                    textExtractor ?? defaultTextExtractor,
                    CreateFieldThesaurus(defaultTokenizer, tokenizer, defaultThesaurusBuilder, thesaurusOptions),
                    scoreBoost));

            return this;
        }

        /// <inheritdoc cref="WithDynamicFields{TChild}(string, Func{TObject, ICollection{TChild}}, Func{TChild, string}, Func{TChild, string}, string?, Func{TokenizerBuilder, TokenizerBuilder}?, ITextExtractor?, Func{ThesaurusBuilder, ThesaurusBuilder}?, double)"/>
        public ObjectTokenizationBuilder<TObject, TKey> WithDynamicFields<TChild>(
            string dynamicFieldReaderName,
            Func<TObject, ICollection<TChild>?> dynamicFieldReader,
            Func<TChild, string> getFieldName,
            Func<TChild, IEnumerable<string>> getFieldText,
            string? fieldNamePrefix = null,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null,
            Func<ThesaurusBuilder, ThesaurusBuilder>? thesaurusOptions = null,
            double scoreBoost = 1D)
        {
            if (dynamicFieldReader == null)
            {
                throw new ArgumentNullException(nameof(dynamicFieldReader));
            }

            var tokenizer = tokenizationOptions.CreateTokenizer();
            this.dynamicFieldReaderBuilders.Add(
                (defaultTokenizer, defaultThesaurusBuilder, defaultTextExtractor) => new StringArrayChildObjectDynamicFieldReader<TObject, TChild>(
                    dynamicFieldReader,
                    getFieldName,
                    getFieldText,
                    dynamicFieldReaderName,
                    fieldNamePrefix,
                    tokenizer ?? defaultTokenizer,
                    textExtractor ?? defaultTextExtractor,
                    CreateFieldThesaurus(defaultTokenizer, tokenizer, defaultThesaurusBuilder, thesaurusOptions),
                    scoreBoost));

            return this;
        }

        /// <summary>
        /// Adds a field to be indexed for the object.
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
        /// An optional delegate capable of building the thesaurus for this field. If this is unspecified then the default thesaurus
        /// for the index will be used.
        /// </param>
        /// <param name="scoreBoost">
        /// The multiplier to apply to the score of this field when ranking results. The default value of 1 is equivalent to no boosting.
        /// </param>
        public ObjectTokenizationBuilder<TObject, TKey> WithField(
            string name,
            Func<TObject, IEnumerable<string>> reader,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null,
            Func<ThesaurusBuilder, ThesaurusBuilder>? thesaurusOptions = null,
            double scoreBoost = 1D)
        {
            ValidateFieldParameters(name, reader);
            var tokenizer = tokenizationOptions.CreateTokenizer();
            this.fieldReaderBuilders.Add((defaultTokenizer, defaultThesaurusBuilder, defaultTextExtractor) =>
                new StringArrayFieldReader<TObject>(
                    name,
                    reader,
                    tokenizer ?? defaultTokenizer,
                    textExtractor ?? defaultTextExtractor,
                    CreateFieldThesaurus(defaultTokenizer, tokenizer, defaultThesaurusBuilder, thesaurusOptions),
                    scoreBoost));

            return this;
        }

        /// <summary>
        /// Adds a field to be indexed for the object.
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
        /// An optional delegate capable of building the thesaurus for this field. If this is unspecified then the default thesaurus
        /// for the index will be used.
        /// </param>
        /// <param name="scoreBoost">
        /// The multiplier to apply to the score of this field when ranking results. The default value of 1 is equivalent to no boosting.
        /// </param>
        public ObjectTokenizationBuilder<TObject, TKey> WithField(
            string name,
            Func<TObject, CancellationToken, Task<string>> fieldTextReader,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null,
            Func<ThesaurusBuilder, ThesaurusBuilder>? thesaurusOptions = null,
            double scoreBoost = 1D)
        {
            ValidateFieldParameters(name, fieldTextReader);
            var tokenizer = tokenizationOptions.CreateTokenizer();
            this.fieldReaderBuilders.Add((defaultTokenizer, defaultThesaurusBuilder, defaultTextExtractor) =>
                new AsyncStringFieldReader<TObject>(
                    name,
                    fieldTextReader,
                    tokenizer ?? defaultTokenizer,
                    textExtractor ?? defaultTextExtractor,
                    CreateFieldThesaurus(defaultTokenizer, tokenizer, defaultThesaurusBuilder, thesaurusOptions),
                    scoreBoost));

            return this;
        }

        /// <inheritdoc cref="WithField(string, Func{TObject, CancellationToken, Task{string}}, Func{TokenizerBuilder, TokenizerBuilder}?, ITextExtractor?, Func{ThesaurusBuilder, ThesaurusBuilder}?, double)"/>
        public ObjectTokenizationBuilder<TObject, TKey> WithField(
            string name,
            Func<TObject, Task<string>> fieldTextReader,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null,
            Func<ThesaurusBuilder, ThesaurusBuilder>? thesaurusOptions = null,
            double scoreBoost = 1D)
        {
            return this.WithField(
                name,
                (item, ctx) => fieldTextReader(item),
                tokenizationOptions,
                textExtractor,
                thesaurusOptions,
                scoreBoost);
        }

        /// <summary>
        /// Adds a field to be indexed for the object.
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
        /// An optional delegate capable of building the thesaurus for this field. If this is unspecified then the default thesaurus
        /// for the index will be used.
        /// </param>
        /// <param name="scoreBoost">
        /// The multiplier to apply to the score of this field when ranking results. The default value of 1 is equivalent to no boosting.
        /// </param>
        public ObjectTokenizationBuilder<TObject, TKey> WithField(
            string name,
            Func<TObject, CancellationToken, Task<IEnumerable<string>>> fieldTextReader,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            ITextExtractor? textExtractor = null,
            Func<ThesaurusBuilder, ThesaurusBuilder>? thesaurusOptions = null,
            double scoreBoost = 1D)
        {
            ValidateFieldParameters(name, fieldTextReader);
            var tokenizer = tokenizationOptions.CreateTokenizer();
            this.fieldReaderBuilders.Add((defaultTokenizer, defaultThesaurusBuilder, defaultTextExtractor) =>
                new AsyncStringArrayFieldReader<TObject>(
                    name,
                    fieldTextReader,
                    tokenizer ?? defaultTokenizer,
                    textExtractor ?? defaultTextExtractor,
                    CreateFieldThesaurus(defaultTokenizer, tokenizer, defaultThesaurusBuilder, thesaurusOptions),
                    scoreBoost));

            return this;
        }

        /// <inheritdoc cref="WithField(string, Func{TObject, CancellationToken, Task{IEnumerable{string}}}, Func{TokenizerBuilder, TokenizerBuilder}?, ITextExtractor?, Func{ThesaurusBuilder, ThesaurusBuilder}?, double)" />
        public ObjectTokenizationBuilder<TObject, TKey> WithField(
            string name,
            Func<TObject, Task<IEnumerable<string>>> fieldTextReader,
            Func<TokenizerBuilder, TokenizerBuilder>? tokenizationOptions = null,
            Func<ThesaurusBuilder, ThesaurusBuilder>? thesaurusOptions = null,
            ITextExtractor? textExtractor = null,
            double scoreBoost = 1D)
        {
            return this.WithField(
                name,
                (item, ctx) => fieldTextReader(item),
                tokenizationOptions,
                textExtractor,
                thesaurusOptions,
                scoreBoost);
        }

        /// <inheritdoc />
        public ObjectTokenizationBuilder<TObject, TKey> WithScoreBoosting(Action<ObjectScoreBoostBuilder<TObject>> scoreBoostingOptions)
        {
            if (scoreBoostingOptions is null)
            {
                throw new ArgumentNullException(nameof(scoreBoostingOptions));
            }

            if (this.objectScoreBoostBuilder is not null)
            {
                throw new LiftiException(ExceptionMessages.WithScoreBoostingCanOnlyBeCalledOncePerObjectDefinition);
            }

            this.objectScoreBoostBuilder = new ObjectScoreBoostBuilder<TObject>();
            scoreBoostingOptions(this.objectScoreBoostBuilder);
            return this;
        }

        private static Thesaurus CreateFieldThesaurus(
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

        /// <inheritdoc />
        IObjectTypeConfiguration IObjectTokenizationBuilder.Build(
            byte objectTypeId,
            IIndexTokenizer defaultTokenizer,
            ThesaurusBuilder defaultThesaurusBuilder,
            ITextExtractor defaultTextExtractor,
            IndexedFieldLookup fieldLookup)
        {
            if (this.keyReader == null)
            {
                throw new LiftiException(ExceptionMessages.KeyReaderMustBeProvided);
            }

            if (this.fieldReaderBuilders.Count == 0 && this.dynamicFieldReaderBuilders.Count == 0)
            {
                throw new LiftiException(ExceptionMessages.AtLeastOneFieldMustBeIndexed);
            }

            var staticFields = this.fieldReaderBuilders.Select(builder => builder(defaultTokenizer, defaultThesaurusBuilder, defaultTextExtractor)).ToList();
            foreach (var staticField in staticFields)
            {
                fieldLookup.RegisterStaticField(staticField);
            }

            var dynamicFieldReaders = this.dynamicFieldReaderBuilders.Select(builder => builder(defaultTokenizer, defaultThesaurusBuilder, defaultTextExtractor)).ToList();
            foreach (var dynamicFieldReader in dynamicFieldReaders)
            {
                fieldLookup.RegisterDynamicFieldReader(dynamicFieldReader);
            }

            return new ObjectTypeConfiguration<TObject, TKey>(
                objectTypeId,
                this.keyReader,
                staticFields,
                dynamicFieldReaders,
                this.objectScoreBoostBuilder?.Build() ?? ObjectScoreBoostOptions<TObject>.Empty());
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
