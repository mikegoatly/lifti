using Lifti.Querying.QueryParts;
using Lifti.Tokenization;
using System;
using System.Collections.Generic;
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
        public IQuery Parse(IIndexedFieldLookup fieldLookup, string queryText, IIndexTokenizer tokenizer)
        {
            if (fieldLookup is null)
            {
                throw new ArgumentNullException(nameof(fieldLookup));
            }

            IQueryPart? rootPart = null;

            var state = new QueryParserState(this.queryTokenizer, tokenizer, queryText);
            while (state.TryGetNextToken(out var token))
            {
                rootPart = this.CreateQueryPart(fieldLookup, state, token, tokenizer, rootPart);
            }

            return new Query(rootPart ?? EmptyQueryPart.Instance);
        }

        private IQueryPart CreateQueryPart(
            IIndexedFieldLookup fieldLookup,
            QueryParserState state,
            QueryToken token,
            IIndexTokenizer tokenizer,
            IQueryPart? currentQuery)
        {
            switch (token.TokenType)
            {
                case QueryTokenType.Text:
                    return this.ComposePart(currentQuery, this.CreateWordQueryPart(token, tokenizer));

                case QueryTokenType.FieldFilter:
                    var (fieldId, _, fieldTokenizer) = fieldLookup.GetFieldInfo(token.TokenText);
                    var filteredPart = this.CreateQueryPart(fieldLookup, state, state.GetNextToken(), fieldTokenizer, null);
                    return this.ComposePart(
                        currentQuery,
                        new FieldFilterQueryOperator(token.TokenText, fieldId, filteredPart));

                case QueryTokenType.OrOperator:
                case QueryTokenType.AndOperator:
                case QueryTokenType.NearOperator:
                case QueryTokenType.PrecedingNearOperator:
                case QueryTokenType.PrecedingOperator:
                    var rightPart = this.CreateQueryPart(fieldLookup, state, state.GetNextToken(), tokenizer, null);
                    return CombineParts(currentQuery, rightPart, token.TokenType, token.Tolerance);

                case QueryTokenType.OpenBracket:
                    var bracketedPart = state.GetTokensUntil(QueryTokenType.CloseBracket)
                        .Aggregate((IQueryPart?)null, (current, next) => this.CreateQueryPart(fieldLookup, state, next, tokenizer, current));

                    if (bracketedPart == null)
                    {
                        throw new QueryParserException(ExceptionMessages.EmptyBracketedExpressionsAreNotSupported);
                    }

                    return this.ComposePart(currentQuery, new BracketedQueryPart(bracketedPart));

                case QueryTokenType.BeginAdjacentTextOperator:
                    var tokens = state.GetTokensUntil(QueryTokenType.EndAdjacentTextOperator)
                        .Select(t => this.CreateWordQueryPart(t, tokenizer))
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

        private IQueryPart CreateWordQueryPart(QueryToken queryToken, IIndexTokenizer tokenizer)
        {
            if (queryToken.TokenType != QueryTokenType.Text)
            {
                throw new QueryParserException(ExceptionMessages.ExpectedTextToken, queryToken.TokenType);
            }

            var tokenText = queryToken.TokenText.AsSpan();

            var fuzzyMatchInfo = ExplicitFuzzySearchTerm.Parse(tokenText);

            if (!fuzzyMatchInfo.IsFuzzyMatch && WildcardQueryPartParser.TryParse(tokenText, tokenizer, out var wildcardQueryPart))
            {
                return wildcardQueryPart;
            }

            // We hand off any matched text in the query to the tokenizer (either the index default,
            // or the one associated to the specific field being queryied) because we need to ensure
            // that it is:
            // a) Normalized in the same way as the tokens as they were added to the index
            // b) Any additional processing, e.g. stemming is applied to them
            IEnumerable<IQueryPart> result;
            if (fuzzyMatchInfo.IsFuzzyMatch || this.options.AssumeFuzzySearchTerms)
            {
                result = tokenizer.Process(fuzzyMatchInfo.IsFuzzyMatch ? tokenText.Slice(fuzzyMatchInfo.SearchTermStartIndex) : tokenText)
                 .Select(
                    token => new FuzzyMatchQueryPart(
                        token.Value,
                        fuzzyMatchInfo.MaxEditDistance ?? this.options.FuzzySearchMaxEditDistance(token.Value.Length),
                        fuzzyMatchInfo.MaxSequentialEdits ?? this.options.FuzzySearchMaxSequentialEdits(token.Value.Length)));
            }
            else
            {
                result = tokenizer.Process(tokenText)
                     .Select(token => new ExactWordQueryPart(token.Value));
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
                QueryTokenType.OrOperator => OperatorPrecedence.Or,
                QueryTokenType.PrecedingNearOperator 
                    or QueryTokenType.NearOperator 
                    or QueryTokenType.PrecedingOperator => OperatorPrecedence.Positional,
                _ => throw new QueryParserException(ExceptionMessages.UnexpectedOperatorInternal, tokenType),
            };
        }

        private class QueryParserState
        {
            private readonly IEnumerator<QueryToken> enumerator;

            public QueryParserState(IQueryTokenizer queryTokenizer, IIndexTokenizer tokenizer, string queryText)
            {
                this.enumerator = queryTokenizer.ParseQueryTokens(queryText, tokenizer).GetEnumerator();
            }

            public bool TryGetNextToken(out QueryToken token)
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
