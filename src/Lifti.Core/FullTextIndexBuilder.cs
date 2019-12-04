using Lifti.ItemTokenization;
using Lifti.Querying;
using Lifti.Tokenization;
using System;

namespace Lifti
{
    public class FullTextIndexBuilder<TKey>
    {
        private readonly ConfiguredItemTokenizationOptions<TKey> itemTokenizationOptions = new ConfiguredItemTokenizationOptions<TKey>();
        private readonly AdvancedOptions advancedOptions = new AdvancedOptions();
        private IIndexNodeFactory indexNodeFactory;
        private ITokenizerFactory tokenizerFactory;
        private IQueryParser queryParser;
        private TokenizationOptions defaultTokenizationOptions = TokenizationOptions.Default;

        /// <summary>
        /// Creates an <see cref="ItemTokenizationOptions{TItem, TKey}"/> configuration entry for an item of type <typeparamref name="TItem"/>
        /// in the index.
        /// </summary>
        /// <param name="idReader">
        /// A delegate capable of specifying all the required options for the item tokenization options.
        /// </param>
        public FullTextIndexBuilder<TKey> WithItemTokenization<TItem>(Func<ItemTokenizationOptionsBuilder<TItem, TKey>, ItemTokenizationOptionsBuilder<TItem, TKey>> optionsBuilder)
        {
            if (optionsBuilder is null)
            {
                throw new ArgumentNullException(nameof(optionsBuilder));
            }

            var builder = new ItemTokenizationOptionsBuilder<TItem, TKey>();
            this.itemTokenizationOptions.Add(optionsBuilder(builder).Build());

            return this;
        }

        /// <summary>
        /// Specifies the default tokenization options that should be used when searching or indexing
        /// when no other options are provided.
        /// </summary>
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
        /// intra-node text completely, set this to an arbitrarily large value, e.g. <see cref="Int32.MaxValue"/>.
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

        public FullTextIndex<TKey> Build()
        {
            this.indexNodeFactory = this.indexNodeFactory ?? new IndexNodeFactory();

            this.indexNodeFactory.Configure(this.advancedOptions);

            return new FullTextIndex<TKey>(
                this.itemTokenizationOptions,
                this.indexNodeFactory,
                this.tokenizerFactory ?? new TokenizerFactory(),
                this.queryParser ?? new QueryParser(),
                this.defaultTokenizationOptions,
                null);
        }
    }
}