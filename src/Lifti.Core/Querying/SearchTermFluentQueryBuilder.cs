using Lifti.Querying.QueryParts;
using Lifti.Tokenization;
using System;
using System.Collections.Generic;

namespace Lifti.Querying
{
    /// <summary>
    /// </summary>
    public sealed class FluentQueryBuilder<TKey>
        where TKey : notnull
    {
        private readonly SearchTermFluentQueryBuilder<TKey> searchTermBuilder;
        private BinaryOperatorFluentQueryBuilder<TKey>? operatorBuilder;

        internal FluentQueryBuilder(IFullTextIndex<TKey> index)
            : this(index, index.DefaultTokenizer)
        {
        }

        internal FluentQueryBuilder(IFullTextIndex<TKey> index, IIndexTokenizer tokenizer)
        {
            this.Index = index;
            this.CurrentTokenizer = tokenizer;
            this.searchTermBuilder = new SearchTermFluentQueryBuilder<TKey>(this);
        }

        internal IIndexTokenizer CurrentTokenizer { get; }
        internal IFullTextIndex<TKey> Index { get; }
        internal IQueryPart CurrentPart { get; set; } = EmptyQueryPart.Instance;

        /// <summary>
        /// Combines the current query with the next using the AND (&amp;) operator.
        /// </summary>
        public BinaryOperatorFluentQueryBuilder<TKey> And => this.CreateBinaryOperatorBuilder(static (current, next) => new AndQueryOperator(current, next));

        /// <summary>
        /// Combines the current query with the next using the OR (|) operator.
        /// </summary>
        public BinaryOperatorFluentQueryBuilder<TKey> Or => this.CreateBinaryOperatorBuilder(static (current, next) => new OrQueryOperator(current, next));

        /// <summary>
        /// Combines the current query with the next using the Preceding (>) operator. The next search term will be required to appear somewhere in the document
        /// after the previous one.
        /// </summary>
        public BinaryOperatorFluentQueryBuilder<TKey> Preceding => this.CreateBinaryOperatorBuilder(static (current, next) => new PrecedingQueryOperator(current, next));

        /// <summary>
        /// Combines the current query with the next using the Near (~n) operator, optionally specifying the tolerance for the distance between the matched terms. The
        /// default tolerance is 5. Search terms must be within the specified distance on either side of each other in order to match.
        /// </summary>
        public BinaryOperatorFluentQueryBuilder<TKey> Near(int tolerance = 5) => this.CreateBinaryOperatorBuilder((current, next) => new NearQueryOperator(current, next, tolerance));

        /// <summary>
        /// Combines the current query with the next using the Near Preceding (~n>) operator. The next search term will be required to appear in the document within the specified tolerence 
        /// after the previous one.
        /// </summary>
        public BinaryOperatorFluentQueryBuilder<TKey> CloselyPreceding(int tolerance = 5) => this.CreateBinaryOperatorBuilder((current, next) => new PrecedingNearQueryOperator(current, next, tolerance));

        /// <summary>
        /// Execute the query against the index it was created for, returning the results.
        /// </summary>
        public ISearchResults<TKey> Execute()
        {
            return this.Index.Search(this.Build());
        }

        /// <summary>
        /// Builds the query in its currently built state.
        /// </summary>
        public IQuery Build()
        {
            return new Query(this.CurrentPart);
        }

        internal static SearchTermFluentQueryBuilder<TKey> StartQuery(IFullTextIndex<TKey> index)
        {
            return new FluentQueryBuilder<TKey>(index).searchTermBuilder;
        }

        internal SearchTermFluentQueryBuilder<TKey> StartSubquery()
        {
            return new FluentQueryBuilder<TKey>(this.Index, this.CurrentTokenizer).searchTermBuilder;
        }

        internal SearchTermFluentQueryBuilder<TKey> StartSubquery(IIndexTokenizer tokenizer)
        {
            return new FluentQueryBuilder<TKey>(this.Index, tokenizer).searchTermBuilder;
        }

        internal BinaryOperatorFluentQueryBuilder<TKey> CreateBinaryOperatorBuilder(Func<IQueryPart, IQueryPart, BinaryQueryOperator> builder)
        {
            this.operatorBuilder ??= new BinaryOperatorFluentQueryBuilder<TKey>(this);
            this.operatorBuilder.Builder = builder;
            return this.operatorBuilder;
        }

        internal string NormalizeText(string text)
        {
            // Prepare the text for the index using the default tokenizer
#if NETSTANDARD
            text = this.CurrentTokenizer.Normalize(text.AsSpan());
#else
            text = this.CurrentTokenizer.Normalize(text);
#endif
            return text;
        }
    }

    /// <summary>
    /// </summary>
    public class SearchTermFluentQueryBuilder<TKey>
        where TKey : notnull
    {
        internal SearchTermFluentQueryBuilder(FluentQueryBuilder<TKey> parentBuilder)
        {
            this.ParentBuilder = parentBuilder;
        }

        internal FluentQueryBuilder<TKey> ParentBuilder { get; }

        /// <summary>
        /// Builds a part of the query where all the terms must appear sequentially.
        /// </summary>
        public FluentQueryBuilder<TKey> Adjacent(Func<BaseAdjacentSearchTermFluentQueryBuilder<TKey>, AdjacentSearchTermFluentQueryBuilder<TKey>> sequentialBuilder)
        {
            if (sequentialBuilder is null)
            {
                throw new ArgumentNullException(nameof(sequentialBuilder));
            }

            var builtQuery = sequentialBuilder(new AdjacentSearchTermFluentQueryBuilder<TKey>(this.ParentBuilder));

            return this.CombineQueryPart(new AdjacentWordsQueryOperator(builtQuery.Parts));
        }

        /// <summary>
        /// Adds a part of the query restricts the search results to a specific field.
        /// </summary>
        /// <param name="fieldName">
        /// The name of the field to restrict the search to.
        /// </param>
        /// <param name="fieldQueryBuilder">
        /// A function that builds the query to apply to the field.
        /// </param>
        public FluentQueryBuilder<TKey> InField(string fieldName, Func<SearchTermFluentQueryBuilder<TKey>, FluentQueryBuilder<TKey>> fieldQueryBuilder)
        {
            if (fieldQueryBuilder is null)
            {
                throw new ArgumentNullException(nameof(fieldQueryBuilder));
            }

            // Find the field id for the field
            var fieldInfo = this.ParentBuilder.Index.FieldLookup.GetFieldInfo(fieldName);

            var builtFieldQuery = fieldQueryBuilder(this.ParentBuilder.StartSubquery(fieldInfo.Tokenizer));

            var childPart = builtFieldQuery.CurrentPart;
            
            // If more than just a single term was added, wrap the query in a bracketed query part
            if (childPart is not WordQueryPart and not BracketedQueryPart)
            {
                childPart = new BracketedQueryPart(childPart);
            }

            return this.CombineQueryPart(new FieldFilterQueryOperator(fieldName, fieldInfo.Id, childPart));
        }

        /// <summary>
        /// Adds a bracketed part to the query.
        /// </summary>
        /// <param name="bracketedQueryBuilder">
        /// A function that builds the query to contain in the bracketed section.
        /// </param>
        public FluentQueryBuilder<TKey> Bracketed(Func<SearchTermFluentQueryBuilder<TKey>, FluentQueryBuilder<TKey>> bracketedQueryBuilder)
        {
            if (bracketedQueryBuilder is null)
            {
                throw new ArgumentNullException(nameof(bracketedQueryBuilder));
            }

            var buildQuery = bracketedQueryBuilder(this.ParentBuilder.StartSubquery());

            var childPart = buildQuery.CurrentPart;

            return this.CombineQueryPart(new BracketedQueryPart(childPart));
        }

        /// <summary>
        /// Adds an exact word search term to the query.
        /// </summary>
        /// <param name="text">The text to search for.</param>
        /// <param name="scoreBoost">The score boost to apply to documents that match this term. A null value indicates no additional score boost should be applied.</param>
        public FluentQueryBuilder<TKey> ExactMatch(string text, double? scoreBoost = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("The text to match cannot be null or empty.", nameof(text));
            }

            var part = new ExactWordQueryPart(this.ParentBuilder.NormalizeText(text), scoreBoost);

            return this.CombineQueryPart(part);
        }

        /// <summary>
        /// Adds a fuzzy match search term to the query.
        /// </summary>
        /// <param name="text">The text to perform a fuzzy match with.</param>
        /// <param name="maxEditDistance">The maximum of edits allowed for any given match. The higher this value, the more divergent 
        /// matches will be.</param>
        /// <param name="maxSequentialEdits">The maximum number of edits that are allowed to appear sequentially. By default this is 1,
        /// which forces matches to be more similar to the search criteria.</param>
        /// <param name="scoreBoost">The score boost to apply to documents that match this term. A null value indicates no additional score boost should be applied.</param>
        /// <returns></returns>
        public FluentQueryBuilder<TKey> FuzzyMatch(
            string text, 
            ushort maxEditDistance = FuzzyMatchQueryPart.DefaultMaxEditDistance, 
            ushort maxSequentialEdits = FuzzyMatchQueryPart.DefaultMaxSequentialEdits, 
            double? scoreBoost = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("The text to match cannot be null or empty.", nameof(text));
            }

            var part = new FuzzyMatchQueryPart(this.ParentBuilder.NormalizeText(text), maxEditDistance, maxSequentialEdits, scoreBoost);

            return this.CombineQueryPart(part);
        }

        /// <summary>
        /// Adds a wildcard match search term to the query.
        /// </summary>
        /// <param name="wildcardBuilder">
        /// A function that builds the wildcard query.
        /// </param>
        /// <param name="scoreBoost">
        /// The score boost to apply to documents that match this term. A null value indicates no additional score boost should be applied.
        /// </param>
        public FluentQueryBuilder<TKey> WildcardMatch(Func<BaseWildcardBuilder<TKey>, WildcardBuilder<TKey>> wildcardBuilder, double? scoreBoost = null)
        {
            if (wildcardBuilder is null)
            {
                throw new ArgumentNullException(nameof(wildcardBuilder));
            }

            var builtQuery = wildcardBuilder(new WildcardBuilder<TKey>(this.ParentBuilder));

            return this.CombineQueryPart(new WildcardQueryPart(builtQuery.Fragments, scoreBoost));
        }

        /// <summary>
        /// Adds a wildcard match search term to the query.
        /// </summary>
        /// <param name="text">
        /// A the wildcard pattern to search with. * indicates multiple characters, and % indicates a single character. All other text will be matched exactly.
        /// </param>
        /// <param name="scoreBoost">
        /// The score boost to apply to documents that match this term. A null value indicates no additional score boost should be applied.
        /// </param>
        public FluentQueryBuilder<TKey> WildcardMatch(string text, double? scoreBoost = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("The text to match cannot be null or empty.", nameof(text));
            }

#if NETSTANDARD
            if (!WildcardQueryPartParser.TryParse(text.AsSpan(), this.ParentBuilder.CurrentTokenizer, scoreBoost, out var parsedPart))
#else
            if (!WildcardQueryPartParser.TryParse(text, this.ParentBuilder.CurrentTokenizer, scoreBoost, out var parsedPart))
#endif
            {
                throw new ArgumentException($"The wildcard query '{text}' could not be parsed.",nameof(text));
            }

            return this.CombineQueryPart(parsedPart);
        }

        internal virtual IQueryPart CreateQueryPart(IQueryPart queryPart)
        {
            return queryPart;
        }

        private FluentQueryBuilder<TKey> CombineQueryPart(IQueryPart part)
        {
            this.ParentBuilder.CurrentPart = this.CreateQueryPart(part);

            return this.ParentBuilder;
        }
    }

    /// <summary>
    /// </summary>
    public abstract class BaseWildcardBuilder<TKey>
        where TKey : notnull
    {
        private readonly FluentQueryBuilder<TKey> parentBuilder;

        internal BaseWildcardBuilder(FluentQueryBuilder<TKey> parentBuilder)
        {
            this.parentBuilder = parentBuilder;
        }

        internal List<WildcardQueryFragment> Fragments { get; } = [];

        /// <summary>
        /// Adds a pattern to the wildcard query that matches multiple characters (*).
        /// </summary>
        public WildcardBuilder<TKey> MultipleCharacters()
        {
            return this.AddPattern(WildcardQueryFragment.MultiCharacter);
        }

        /// <summary>
        /// Adds a pattern to the wildcard query that matches a single character (%).
        /// </summary>
        public WildcardBuilder<TKey> SingleCharacter()
        {
            return this.AddPattern(WildcardQueryFragment.SingleCharacter);
        }

        /// <summary>
        /// Adds a pattern to the wildcard query that matches the specified text.
        /// </summary>
        public WildcardBuilder<TKey> Text(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("The text to match cannot be null or empty.", nameof(text));
            }

            return this.AddPattern(WildcardQueryFragment.CreateText(this.parentBuilder.NormalizeText(text)));
        }

        internal abstract WildcardBuilder<TKey> AddPattern(WildcardQueryFragment pattern);
    }

    /// <summary>
    /// </summary>
    public class WildcardBuilder<TKey> : BaseWildcardBuilder<TKey>
        where TKey : notnull
    {
        internal WildcardBuilder(FluentQueryBuilder<TKey> parentBuilder)
            : base(parentBuilder)
        {
        }

        internal override WildcardBuilder<TKey> AddPattern(WildcardQueryFragment pattern)
        {
            this.Fragments.Add(pattern);
            return this;
        }
    }

    /// <summary>
    /// </summary>
    public sealed class AdjacentSearchTermFluentQueryBuilder<TKey> : BaseAdjacentSearchTermFluentQueryBuilder<TKey>
        where TKey : notnull
    {
        internal AdjacentSearchTermFluentQueryBuilder(FluentQueryBuilder<TKey> parentBuilder)
            : base(parentBuilder)
        {
        }

        internal override AdjacentSearchTermFluentQueryBuilder<TKey> AddQueryPart(IQueryPart part)
        {
            this.Parts.Add(part);
            return this;
        }
    }

    /// <summary>
    /// </summary>
    public abstract class BaseAdjacentSearchTermFluentQueryBuilder<TKey>
        where TKey : notnull
    {
        private readonly FluentQueryBuilder<TKey> parentBuilder;

        internal BaseAdjacentSearchTermFluentQueryBuilder(FluentQueryBuilder<TKey> parentBuilder)
        {
            this.parentBuilder = parentBuilder;
        }

        internal List<IQueryPart> Parts { get; } = [];

        /// <inheritdoc cref="SearchTermFluentQueryBuilder{TKey}.ExactMatch(string, double?)"/>
        public AdjacentSearchTermFluentQueryBuilder<TKey> ExactMatch(string text, double? scoreBoost = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("The text to match cannot be null or empty.", nameof(text));
            }

            var part = new ExactWordQueryPart(this.parentBuilder.NormalizeText(text), scoreBoost);

            return this.AddQueryPart(part);
        }

        /// <inheritdoc cref="SearchTermFluentQueryBuilder{TKey}.FuzzyMatch(string, ushort, ushort, double?)"/>
        public AdjacentSearchTermFluentQueryBuilder<TKey> FuzzyMatch(
            string text,
            ushort maxEditDistance = FuzzyMatchQueryPart.DefaultMaxEditDistance,
            ushort maxSequentialEdits = FuzzyMatchQueryPart.DefaultMaxSequentialEdits,
            double? scoreBoost = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("The text to match cannot be null or empty.", nameof(text));
            }

            var part = new FuzzyMatchQueryPart(this.parentBuilder.NormalizeText(text), maxEditDistance, maxSequentialEdits, scoreBoost);

            return this.AddQueryPart(part);
        }

        /// <inheritdoc cref="SearchTermFluentQueryBuilder{TKey}.WildcardMatch(Func{BaseWildcardBuilder{TKey}, WildcardBuilder{TKey}}, double?)"/>
        public AdjacentSearchTermFluentQueryBuilder<TKey> WildcardMatch(Func<BaseWildcardBuilder<TKey>, WildcardBuilder<TKey>> wildcardBuilder, double? scoreBoost = null)
        {
            if (wildcardBuilder is null)
            {
                throw new ArgumentNullException(nameof(wildcardBuilder));
            }

            var builtQuery = wildcardBuilder(new WildcardBuilder<TKey>(this.parentBuilder));

            return this.AddQueryPart(new WildcardQueryPart(builtQuery.Fragments, scoreBoost));
        }

        /// <inheritdoc cref="SearchTermFluentQueryBuilder{TKey}.WildcardMatch(string, double?)"/>
        public AdjacentSearchTermFluentQueryBuilder<TKey> WildcardMatch(string text, double? scoreBoost = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("The text to match cannot be null or empty.", nameof(text));
            }

#if NETSTANDARD
            if (!WildcardQueryPartParser.TryParse(text.AsSpan(), this.parentBuilder.CurrentTokenizer, scoreBoost, out var parsedPart))
#else
            if (!WildcardQueryPartParser.TryParse(text, this.parentBuilder.CurrentTokenizer, scoreBoost, out var parsedPart))
#endif
            {
                throw new ArgumentException($"The wildcard query '{text}' could not be parsed.", nameof(text));
            }

            return this.AddQueryPart(parsedPart);
        }

        internal abstract AdjacentSearchTermFluentQueryBuilder<TKey> AddQueryPart(IQueryPart part);
    }


    /// <summary>
    /// </summary>
    public sealed class BinaryOperatorFluentQueryBuilder<TKey> : SearchTermFluentQueryBuilder<TKey>
        where TKey : notnull
    {
        internal BinaryOperatorFluentQueryBuilder(FluentQueryBuilder<TKey> parentBuilder)
            : base(parentBuilder)
        {
        }

        internal Func<IQueryPart, IQueryPart, BinaryQueryOperator> Builder { get; set; } = null!;

        internal override IQueryPart CreateQueryPart(IQueryPart queryPart)
        {
            return this.Builder(this.ParentBuilder.CurrentPart, queryPart);
        }
    }
}
