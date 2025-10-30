using Lifti.Querying.QueryParts;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Lifti.Querying
{
    /// <inheritdoc />
    internal class QueryParser : IQueryParser
    {
        private readonly IQueryTokenizer queryTokenizer;
        private readonly QueryParserOptions options;
        private readonly QueryTokenType defaultJoiningOperator;

        public QueryParser(QueryParserOptions options)
        {
            this.queryTokenizer = new QueryTokenizer();
            this.options = options;
            this.defaultJoiningOperator = options.DefaultJoiningOperator switch
            {
                QueryTermJoinOperatorKind.And => QueryTokenType.AndOperator,
                QueryTermJoinOperatorKind.Or => QueryTokenType.OrOperator,
                _ => throw new QueryParserException(ExceptionMessages.UnsupportedQueryJoiningOperator, options.DefaultJoiningOperator)
            };
        }

        /// <inheritdoc />
        public IQuery Parse(IIndexedFieldLookup fieldLookup, string queryText, IIndexTokenizerProvider tokenizerProvider)
        {
            if (fieldLookup is null)
            {
                throw new ArgumentNullException(nameof(fieldLookup));
            }

            IQueryPart? rootPart = null;

            var state = new QueryParserState(this.queryTokenizer, tokenizerProvider, queryText);
            while (state.TryGetNextToken(out var token))
            {
                rootPart = this.CreateQueryPart(fieldLookup, state, token, rootPart);
            }

            return new Query(rootPart ?? EmptyQueryPart.Instance);
        }

        private IQueryPart CreateQueryPart(
            IIndexedFieldLookup fieldLookup,
            QueryParserState state,
            QueryToken token,
            IQueryPart? currentQuery)
        {
            switch (token.TokenType)
            {
                case QueryTokenType.Text:
                    return this.ComposePart(currentQuery, this.CreateWordQueryPart(token));

                case QueryTokenType.FieldFilter:
                    var fieldId = fieldLookup.GetFieldInfo(token.TokenText).Id;
                    var filteredPart = this.CreateQueryPart(fieldLookup, state, state.GetNextToken(), null);
                    return this.ComposePart(
                        currentQuery,
                        new FieldFilterQueryOperator(token.TokenText, fieldId, filteredPart));

                case QueryTokenType.OrOperator:
                case QueryTokenType.AndOperator:
                case QueryTokenType.AndNotOperator:
                case QueryTokenType.NearOperator:
                case QueryTokenType.PrecedingNearOperator:
                case QueryTokenType.PrecedingOperator:
                    var rightPart = this.CreateQueryPart(fieldLookup, state, state.GetNextToken(), null);
                    return CombineParts(currentQuery, rightPart, token.TokenType, token.Tolerance);

                case QueryTokenType.OpenBracket:
                    var bracketedPart = state.GetTokensUntil(QueryTokenType.CloseBracket)
                        .Aggregate((IQueryPart?)null, (current, next) => this.CreateQueryPart(fieldLookup, state, next, current));

                    if (bracketedPart == null)
                    {
                        throw new QueryParserException(ExceptionMessages.EmptyBracketedExpressionsAreNotSupported);
                    }

                    return this.ComposePart(currentQuery, new BracketedQueryPart(bracketedPart));

                case QueryTokenType.BeginAdjacentTextOperator:
                    var tokens = state.GetTokensUntil(QueryTokenType.EndAdjacentTextOperator)
                        .Select(this.CreateWordQueryPart)
                        .ToList();

                    if (tokens.Count == 0)
                    {
                        throw new QueryParserException(ExceptionMessages.EmptyAdjacentTextPartsAreNotSupported);
                    }

                    return this.ComposePart(currentQuery, new AdjacentWordsQueryOperator(tokens));

                default:
                    throw new QueryParserException(ExceptionMessages.UnexpectedTokenEncountered, token.TokenType);
            }
        }

        private IQueryPart CreateWordQueryPart(QueryToken queryToken)
        {
            if (queryToken.TokenType != QueryTokenType.Text)
            {
                throw new QueryParserException(ExceptionMessages.ExpectedTextToken, queryToken.TokenType);
            }

            var indexTokenizer = queryToken.IndexTokenizer ?? throw new InvalidOperationException(ExceptionMessages.TextTokensMustHaveIndexTokenizers);

            var tokenText = queryToken.TokenText.AsSpan();
            var scoreBoost = queryToken.ScoreBoost;
            var requireStart = queryToken.RequireStart;
            var requireEnd = queryToken.RequireEnd;
            var fuzzyMatchInfo = ExplicitFuzzySearchTerm.Parse(tokenText);

            if (!fuzzyMatchInfo.IsFuzzyMatch && WildcardQueryPartParser.TryParse(
                tokenText,
                indexTokenizer,
                scoreBoost,
                out var wildcardQueryPart))
            {
                return wildcardQueryPart;
            }

            // We hand off any matched text in the query to the tokenizer (either the index default,
            // or the one associated to the specific field being queried) because we need to ensure
            // that it is:
            // a) Normalized in the same way as the tokens as they were added to the index
            // b) Any additional processing, e.g. stemming is applied to them
            IEnumerable<IQueryPart> result;
            if (fuzzyMatchInfo.IsFuzzyMatch || this.options.AssumeFuzzySearchTerms)
            {
                result = indexTokenizer.Process(fuzzyMatchInfo.IsFuzzyMatch ? tokenText.Slice(fuzzyMatchInfo.SearchTermStartIndex) : tokenText)
                 .Select(
                    token => new FuzzyMatchQueryPart(
                        token.Value,
                        fuzzyMatchInfo.MaxEditDistance ?? this.options.FuzzySearchMaxEditDistance(token.Value.Length),
                        fuzzyMatchInfo.MaxSequentialEdits ?? this.options.FuzzySearchMaxSequentialEdits(token.Value.Length),
                        scoreBoost));
            }
            else if (requireStart || requireEnd)
            {
                // Use anchored query part when start or end anchors are present
                result = indexTokenizer.Process(tokenText)
                     .Select(token => new AnchoredWordQueryPart(token.Value, requireStart, requireEnd, scoreBoost));
            }
            else
            {
                result = indexTokenizer.Process(tokenText)
                     .Select(token => new ExactWordQueryPart(token.Value, scoreBoost));
            }

            return ComposeParts(result);
        }

        private IQueryPart ComposeParts(IEnumerable<IQueryPart> parts)
        {
            IQueryPart? result = null;
            var enumerator = parts.GetEnumerator();

            while (enumerator.MoveNext())
            {
                result = result == null
                    ? enumerator.Current
                    : this.ComposePart(result, enumerator.Current);
            }

            return result ?? throw new QueryParserException(ExceptionMessages.ExpectedAtLeastOneQueryPartParsed);
        }

        private IQueryPart ComposePart(IQueryPart? existingPart, IQueryPart newPart)
        {
            return existingPart == null
                ? newPart
                : CombineParts(existingPart, newPart, this.defaultJoiningOperator, 0);
        }

        private static IBinaryQueryOperator CombineParts(IQueryPart? existingPart, IQueryPart newPart, QueryTokenType operatorType, int tolerance)
        {
            if (existingPart == null)
            {
                throw new QueryParserException(ExceptionMessages.UnexpectedOperator, operatorType);
            }

            if (existingPart is IBinaryQueryOperator existingBinaryOperator)
            {
                if (existingBinaryOperator.Precedence >= TokenPrecedence(operatorType))
                {
                    existingBinaryOperator.Right = CreateOperator(operatorType, existingBinaryOperator.Right, newPart, tolerance);
                    return existingBinaryOperator;
                }

                return CreateOperator(operatorType, existingBinaryOperator, newPart, tolerance);
            }

            return CreateOperator(operatorType, existingPart, newPart, tolerance);
        }

        private static IBinaryQueryOperator CreateOperator(QueryTokenType tokenType, IQueryPart leftPart, IQueryPart rightPart, int tolerance)
        {
            return tokenType switch
            {
                QueryTokenType.AndOperator => new AndQueryOperator(leftPart, rightPart),
                QueryTokenType.AndNotOperator => new AndNotQueryOperator(leftPart, rightPart),
                QueryTokenType.OrOperator => new OrQueryOperator(leftPart, rightPart),
                QueryTokenType.NearOperator => new NearQueryOperator(leftPart, rightPart, tolerance),
                QueryTokenType.PrecedingNearOperator => new PrecedingNearQueryOperator(leftPart, rightPart, tolerance),
                QueryTokenType.PrecedingOperator => new PrecedingQueryOperator(leftPart, rightPart),
                _ => throw new QueryParserException(ExceptionMessages.UnexpectedOperatorInternal, tokenType),
            };
        }

        private static OperatorPrecedence TokenPrecedence(QueryTokenType tokenType)
        {
            return tokenType switch
            {
                QueryTokenType.AndOperator => OperatorPrecedence.And,
                QueryTokenType.AndNotOperator => OperatorPrecedence.And,
                QueryTokenType.OrOperator => OperatorPrecedence.Or,
                QueryTokenType.PrecedingNearOperator
                    or QueryTokenType.NearOperator
                    or QueryTokenType.PrecedingOperator => OperatorPrecedence.Positional,
                _ => throw new QueryParserException(ExceptionMessages.UnexpectedOperatorInternal, tokenType),
            };
        }

        private sealed class QueryParserState
        {
            private readonly IEnumerator<QueryToken> enumerator;

            public QueryParserState(IQueryTokenizer queryTokenizer, IIndexTokenizerProvider tokenizerProvider, string queryText)
            {
                this.enumerator = queryTokenizer.ParseQueryTokens(queryText, tokenizerProvider).GetEnumerator();
            }

            public bool TryGetNextToken([NotNullWhen(true)] out QueryToken? token)
            {
                if (this.enumerator.MoveNext())
                {
                    token = this.enumerator.Current;
                    return true;
                }

                token = default;
                return false;
            }

            public QueryToken GetNextToken()
            {
                return this.enumerator.MoveNext()
                    ? this.enumerator.Current
                    : throw new QueryParserException(ExceptionMessages.UnexpectedEndOfQuery);
            }

            /// <summary>
            /// Gets tokens from the token stream until the specified token type is encountered. The terminating
            /// token will be consumed but not returned. If the terminating token is not encountered before the 
            /// end of the token stream, a <see cref="QueryParserException"/> will be thrown.
            /// </summary>
            /// <param name="terminatingToken">The terminating token.</param>
            /// <returns>The tokens that appear before the terminating token.</returns>
            public IEnumerable<QueryToken> GetTokensUntil(QueryTokenType terminatingToken)
            {
                var matchedTerminator = false;
                while (this.enumerator.MoveNext())
                {
                    if (this.enumerator.Current.TokenType == terminatingToken)
                    {
                        matchedTerminator = true;
                        break;
                    }

                    yield return this.enumerator.Current;
                }

                if (!matchedTerminator)
                {
                    throw new QueryParserException(ExceptionMessages.ExpectedToken, terminatingToken);
                }
            }
        }
    }
}
