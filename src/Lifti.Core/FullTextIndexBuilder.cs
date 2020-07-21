using Lifti.Querying;
using Lifti.Tokenization;
using Lifti.Tokenization.Objects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lifti
{
    /// <summary>
    /// The starting point for building an <see cref="IFullTextIndex{TKey}"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of key to be stored in the index.</typeparam>
    public class FullTextIndexBuilder<TKey>
    {
        private readonly ConfiguredObjectTokenizationOptions<TKey> itemTokenizationOptions = new ConfiguredObjectTokenizationOptions<TKey>();
        private readonly IndexOptions advancedOptions = new IndexOptions();
        private IIndexNodeFactory? indexNodeFactory;
        private ITokenizerFactory? tokenizerFactory;
        private IIndexScorerFactory? scorerFactory;
        private IQueryParser? queryParser;
        private TokenizationOptions defaultTokenizationOptions = TokenizationOptions.Default;
        private List<Func<IIndexSnapshot<TKey>, Task>>? indexModifiedActions;

        /// <summary>
        /// Configures the behavior the index should exhibit when an item that already exists in the index is indexed again.
        /// The default value is <see cref="DuplicateItemBehavior.ReplaceItem"/>.
        /// </summary>
        public FullTextIndexBuilder<TKey> WithDuplicateItemBehavior(DuplicateItemBehavior duplicateItemBehavior)
        {
            this.advancedOptions.DuplicateItemBehavior = duplicateItemBehavior;
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
        public FullTextIndexBuilder<TKey> WithIndexModificationAction(Func<IIndexSnapshot<TKey>, Task> asyncAction)
        {
            if (asyncAction is null)
            {
                throw new ArgumentNullException(nameof(asyncAction));
            }

            if (this.indexModifiedActions == null)
            {
                this.indexModifiedActions = new List<Func<IIndexSnapshot<TKey>, Task>>();
            }

            this.indexModifiedActions.Add(asyncAction);

            return this;
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
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (this.indexModifiedActions == null)
            {
                this.indexModifiedActions = new List<Func<IIndexSnapshot<TKey>, Task>>();
            }

            this.indexModifiedActions.Add((snapshot) =>
            {
                action(snapshot);
                return Task.CompletedTask;
            });

            return this;
        }

        /// <summary>
        /// Creates an <see cref="ObjectTokenization{TItem, TKey}"/> configuration entry for an item of type <typeparamref name="TItem"/>
        /// in the index.
        /// </summary>
        /// <param name="optionsBuilder">
        /// A delegate capable of specifying all the required options for the item tokenization options.
        /// </param>
        public FullTextIndexBuilder<TKey> WithObjectTokenization<TItem>(Func<ObjectTokenizationOptionsBuilder<TItem, TKey>, ObjectTokenizationOptionsBuilder<TItem, TKey>> optionsBuilder)
        {
            if (optionsBuilder is null)
            {
                throw new ArgumentNullException(nameof(optionsBuilder));
            }

            var builder = new ObjectTokenizationOptionsBuilder<TItem, TKey>();
            this.itemTokenizationOptions.Add(optionsBuilder(builder).Build());

            return this;
        }

        /// <summary>
        /// Specifies the default tokenization options that should be used when searching or indexing
        /// when no other options are provided.
        /// </summary>
        /// <example>
        /// <![CDATA[
        /// var index = new FullTextIndexBuilder<int>()
        ///.WithDefaultTokenizationOptions(o =>o
        ///    .AccentInsensitive(true) // Default
        ///    .CaseInsensitive(true) // Default
        ///    .SplitOnPunctuation(true) // Default
        ///    .SplitOnCharacters('%', '#', '@')
        ///    .WithStemming(true)
        ///    .XmlContent()
        ///)
        ///.Build();
        ///]]>
        /// </example>
        public FullTextIndexBuilder<TKey> WithDefaultTokenizationOptions(Func<TokenizationOptionsBuilder, TokenizationOptionsBuilder> optionsBuilder)
        {
            if (optionsBuilder is null)
            {
                throw new ArgumentNullException(nameof(optionsBuilder));
            }

            this.defaultTokenizationOptions = optionsBuilder.BuildOptionsOrDefault();

            return this;
        }

        /// <summary>
        /// Sets the depth of the index tree after which intra-node text is supported.
        /// A value of zero indicates that intra-node text is always supported. To disable
        /// intra-node text completely, set this to an arbitrarily large value, e.g. <see cref="int.MaxValue"/>.
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
        /// Replaces the default <see cref="IIndexNodeFactory"/> implementation used creating index nodes.
        /// </summary>
        public FullTextIndexBuilder<TKey> WithIndexNodeFactory(IIndexNodeFactory indexNodeFactory)
        {
            if (indexNodeFactory is null)
            {
                throw new ArgumentNullException(nameof(indexNodeFactory));
            }

            this.indexNodeFactory = indexNodeFactory;

            return this;
        }

        /// <summary>
        /// Replaces the default <see cref="ITokenizerFactory"/> implementation.
        /// </summary>
        public FullTextIndexBuilder<TKey> WithTokenizerFactory(ITokenizerFactory tokenizerFactory)
        {
            if (tokenizerFactory is null)
            {
                throw new ArgumentNullException(nameof(tokenizerFactory));
            }

            this.tokenizerFactory = tokenizerFactory;

            return this;
        }

        /// <summary>
        /// Replaces the default <see cref="IQueryParser"/> implementation used when searching the index.
        /// </summary>
        public FullTextIndexBuilder<TKey> WithQueryParser(IQueryParser queryParser)
        {
            if (queryParser is null)
            {
                throw new ArgumentNullException(nameof(queryParser));
            }

            this.queryParser = queryParser;

            return this;
        }

        /// <summary>
        /// Builds a <see cref="FullTextIndex{TKey}"/> using the configuration applied to this instance.
        /// </summary>
        public FullTextIndex<TKey> Build()
        {
            this.indexNodeFactory ??= new IndexNodeFactory();

            this.indexNodeFactory.Configure(this.advancedOptions);

            return new FullTextIndex<TKey>(
                this.advancedOptions,
                this.itemTokenizationOptions,
                this.indexNodeFactory,
                this.tokenizerFactory ?? new TokenizerFactory(),
                this.queryParser ?? new QueryParser(),
                this.scorerFactory ?? new OkapiBm25ScorerFactory(),
                this.defaultTokenizationOptions,
                this.indexModifiedActions?.ToArray());
        }
    }
}