using Lifti.Querying;
using Lifti.Tokenization;
using Lifti.Tokenization.Objects;
using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti
{
    /// <summary>
    /// The starting point for building an <see cref="IFullTextIndex{TKey}"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of key to be stored in the index.</typeparam>
    public class FullTextIndexBuilder<TKey>
        where TKey : notnull
    {
        private readonly List<IObjectTokenizationBuilder> objectTokenizationBuilders = [];
        private readonly IndexOptions advancedOptions = new();
        private ThesaurusBuilder? defaultThesaurusBuilder;
        private IIndexScorerFactory? scorerFactory;
        private IQueryParser? queryParser;
        private IIndexTokenizer defaultTokenizer = IndexTokenizer.Default;
        private List<Func<IIndexSnapshot<TKey>, CancellationToken, Task>>? indexModifiedActions;
        private ITextExtractor? defaultTextExtractor;

        /// <summary>
        /// Configures the index to use a text extraction process when indexing text. This is useful when
        /// source text contains markup, e,g. for XML/HTML you can use the <see cref="XmlTextExtractor"/>.
        /// </summary>
        public FullTextIndexBuilder<TKey> WithTextExtractor<T>()
            where T : ITextExtractor, new()
        {
            return this.WithTextExtractor(new T());
        }

        /// <summary>
        /// Configures the index to use a text extraction process when indexing text. This is useful when
        /// source text contains markup, e,g. for XML/HTML you can use the <see cref="XmlTextExtractor"/>.
        /// </summary>
        public FullTextIndexBuilder<TKey> WithTextExtractor(ITextExtractor textExtractor)
        {
            this.defaultTextExtractor = textExtractor;
            return this;
        }

        /// <summary>
        /// Configures the behavior of the index when an key that has already been added to the index is indexed again.
        /// The default value is <see cref="DuplicateKeyBehavior.Replace"/>.
        /// </summary>
        public FullTextIndexBuilder<TKey> WithDuplicateKeyBehavior(DuplicateKeyBehavior duplicateKeyBehavior)
        {
            this.advancedOptions.DuplicateKeyBehavior = duplicateKeyBehavior;
            return this;
        }

        /// <summary>
        /// Registers the <see cref="IIndexScorerFactory"/> implementation to use when scoring search results. By default the scorer
        /// will be <see cref="OkapiBm25ScorerFactory"/> with the parameters k1 = 1.2 and b = 0.75.
        /// </summary>
        public FullTextIndexBuilder<TKey> WithScorerFactory(IIndexScorerFactory scorerFactory)
        {
            this.scorerFactory = scorerFactory;
            return this;
        }

        /// <summary>
        /// Registers an async action that needs to occur when mutations to the index are committed and
        /// a new snapshot is generated.
        /// </summary>
        /// <param name="asyncAction">
        /// The async action to execute. The argument is the new snapshot of the index.
        /// </param>
        public FullTextIndexBuilder<TKey> WithIndexModificationAction(Func<IIndexSnapshot<TKey>, CancellationToken, Task> asyncAction)
        {
            ArgumentNullException.ThrowIfNull(asyncAction);

            this.indexModifiedActions ??= [];

            this.indexModifiedActions.Add(asyncAction);

            return this;
        }

        /// <inheritdoc cref="WithIndexModificationAction(Func{IIndexSnapshot{TKey}, CancellationToken, Task})"/>
        public FullTextIndexBuilder<TKey> WithIndexModificationAction(Func<IIndexSnapshot<TKey>, Task> asyncAction)
        {
            ArgumentNullException.ThrowIfNull(asyncAction);

            return this.WithIndexModificationAction((snapshot, ct) => asyncAction(snapshot));
        }

        /// <summary>
        /// Registers an action that needs to occur when mutations to the index are committed and
        /// a new snapshot is generated.
        /// </summary>
        /// <param name="action">
        /// The action to execute. The argument is the new snapshot of the index.
        /// </param>
        /// <remarks>
        /// This is just a convenience wrapper around the async execution.
        /// </remarks>
        public FullTextIndexBuilder<TKey> WithIndexModificationAction(Action<IIndexSnapshot<TKey>> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            return this.WithIndexModificationAction(
                (snapshot, ct) =>
                {
                    action(snapshot);
                    return Task.CompletedTask;
                });
        }

        /// <summary>
        /// Configures the index to support tokenizing text from an object of type <typeparamref name="TObject"/>
        /// in the index.
        /// </summary>
        /// <param name="optionsBuilder">
        /// A delegate capable of configuring an <see cref="ObjectTokenizationBuilder{TObject, TKey}"/> instance.
        /// </param>
        public FullTextIndexBuilder<TKey> WithObjectTokenization<TObject>(Func<ObjectTokenizationBuilder<TObject, TKey>, ObjectTokenizationBuilder<TObject, TKey>> optionsBuilder)
        {
            ArgumentNullException.ThrowIfNull(optionsBuilder);

            var builder = new ObjectTokenizationBuilder<TObject, TKey>();
            this.objectTokenizationBuilders.Add(optionsBuilder(builder));

            return this;
        }

        /// <summary>
        /// Specifies the default tokenization options that should be used when searching or indexing
        /// when no other options are provided.
        /// </summary>
        public FullTextIndexBuilder<TKey> WithDefaultTokenization(Func<TokenizerBuilder, TokenizerBuilder> optionsBuilder)
        {
            ArgumentNullException.ThrowIfNull(optionsBuilder);

            this.defaultTokenizer = optionsBuilder.CreateTokenizer()!;

            return this;
        }

        /// <summary>
        /// Builds the default thesaurus to use for the index. This thesaurus will be used for text added directly to the index and also fields
        /// that have no explicit thesaurus defined for them.
        /// </summary>
        public FullTextIndexBuilder<TKey> WithDefaultThesaurus(Func<ThesaurusBuilder, ThesaurusBuilder> thesaurusBuilder)
        {
            ArgumentNullException.ThrowIfNull(thesaurusBuilder);

            this.defaultThesaurusBuilder = thesaurusBuilder(new ThesaurusBuilder());
            return this;
        }

        /// <summary>
        /// Sets the depth of the index tree after which intra-node text is supported.
        /// A value of zero indicates that intra-node text is always supported. To disable
        /// intra-node text completely, set this to an arbitrarily large value, e.g. <see cref="int.MaxValue"/>.
        /// The default value is <c>4</c>.
        /// </summary>
        public FullTextIndexBuilder<TKey> WithIntraNodeTextSupportedAfterIndexDepth(int depth)
        {
            if (depth < 0)
            {
                throw new ArgumentException(ExceptionMessages.ValueMustNotBeLessThanZero, nameof(depth));
            }

            this.advancedOptions.SupportIntraNodeTextAfterIndexDepth = depth;
            return this;
        }

        /// <summary>
        /// Replaces the default <see cref="IQueryParser"/> implementation used when searching the index.
        /// </summary>
        public FullTextIndexBuilder<TKey> WithQueryParser(IQueryParser queryParser)
        {
            ArgumentNullException.ThrowIfNull(queryParser);

            this.queryParser = queryParser;

            return this;
        }

        /// <inheritdoc cref="WithSimpleQueryParser(Func{QueryParserBuilder, QueryParserBuilder})" />
        public FullTextIndexBuilder<TKey> WithSimpleQueryParser()
        {
            return this.WithSimpleQueryParser(static o => o);
        }

        /// <summary>
        /// Configures the index to use a <see cref="SimpleQueryParser"/> instead of the full LIFTI query parser. Use this for situations where
        /// you don't need the full complexity of the query language and just want user input to be run directly against the index.
        /// This is a convenience method equivalent to <code>WithQueryParser(o => o.WithQueryParserFactory(options => new SimpleQueryParser(options)))</code>.
        /// </summary>
        public FullTextIndexBuilder<TKey> WithSimpleQueryParser(Func<QueryParserBuilder, QueryParserBuilder> optionsBuilder)
        {
            ArgumentNullException.ThrowIfNull(optionsBuilder);

            var builder = new QueryParserBuilder(options => new SimpleQueryParser(options));
            this.queryParser = optionsBuilder(builder).Build();

            return this;
        }

        /// <summary>
        /// Replaces the default <see cref="IQueryParser"/> implementation used when searching the index.
        /// </summary>
        public FullTextIndexBuilder<TKey> WithQueryParser(Func<QueryParserBuilder, QueryParserBuilder> optionsBuilder)
        {
            ArgumentNullException.ThrowIfNull(optionsBuilder);

            var builder = new QueryParserBuilder();
            this.queryParser = optionsBuilder(builder).Build();

            return this;
        }

        /// <summary>
        /// Builds a <see cref="FullTextIndex{TKey}"/> using the configuration applied to this instance.
        /// </summary>
        public FullTextIndex<TKey> Build()
        {
            var thesaurusBuilder = this.defaultThesaurusBuilder ?? new ThesaurusBuilder();
            var textExtractor = this.defaultTextExtractor ?? new PlainTextExtractor();

            // Building the object tokenizers also populates the index's field lookup with
            // any static fields that have been defined.
            var fieldLookup = new IndexedFieldLookup();
            var objectTokenizers = new List<IObjectTypeConfiguration>();

            // Start object type IDs at 1 - 0 is reserved for special use where the indexed document is
            // not associated with a specific object.
            byte objectTypeId = 1;
            foreach (var objectTokenizationBuilder in this.objectTokenizationBuilders)
            {
                // This is a limitation of the current binary serialization implementation. We are reserving
                // 3 bits of the object type ID for whether the object has various scoring metadata associated
                // to it. That leaves us with 5 bits for the object type ID.
                // Having more that 31 different *object types* (not fields) seems a bit of a stretch, so this
                // feels ok as a design constraint for now.
                if (objectTypeId > 31)
                {
                    throw new LiftiException(ExceptionMessages.MaximumNumberOfConfiguredObjectTypesReached);
                }

                var objectTokenizer = objectTokenizationBuilder.Build(
                    objectTypeId++,
                    this.defaultTokenizer,
                    thesaurusBuilder,
                    textExtractor,
                    fieldLookup);

                objectTokenizers.Add(objectTokenizer);
            }

            return new FullTextIndex<TKey>(
                this.advancedOptions,
                new ObjectTypeConfigurationLookup<TKey>(objectTokenizers),
                fieldLookup,
                new IndexNodeFactory(this.advancedOptions),
                this.queryParser ?? new QueryParser(new QueryParserOptions()),
                this.scorerFactory ?? new OkapiBm25ScorerFactory(),
                textExtractor,
                this.defaultTokenizer,
                thesaurusBuilder.Build(this.defaultTokenizer),
                this.indexModifiedActions?.ToArray());
        }
    }
}