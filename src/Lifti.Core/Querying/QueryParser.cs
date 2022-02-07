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
        public QueryParser(QueryParserOptions options)
        {
            this.queryTokenizer = new QueryTokenizer();
            this.options = options;
        }


        /// <inheritdoc />
        public IQuery Parse(IIndexedFieldLookup fieldLookup, string queryText, ITokenizer tokenizer)
        {
            if (fieldLookup is null)
            {
                throw new ArgumentNullException(nameof(fieldLookup));
            }

            IQueryPart? rootPart = null;

            var state = new QueryParserState(this.queryTokenizer, queryText);
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
            ITokenizer tokenizer,
            IQueryPart? currentQuery)
        {
            switch (token.TokenType)
            {
                case QueryTokenType.Text:
                    return ComposePart(currentQuery, this.CreateWordQueryPart(token, tokenizer));

                case QueryTokenType.FieldFilter:
                    var (fieldId, _, fieldTokenizer) = fieldLookup.GetFieldInfo(token.TokenText);
                    var filteredPart = this.CreateQueryPart(fieldLookup, state, state.GetNextToken(), fieldTokenizer, null);
                    return ComposePart(
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

                    return ComposePart(currentQuery, new BracketedQueryPart(bracketedPart));

                case QueryTokenType.BeginAdjacentTextOperator:
                    var tokens = state.GetTokensUntil(QueryTokenType.EndAdjacentTextOperator)
                        .Select(t => this.CreateWordQueryPart(t, tokenizer))
                        .ToList();

                    if (tokens.Count == 0)
                    {
                        throw new QueryParserException(ExceptionMessages.EmptyAdjacentTextPartsAreNotSupported);
                    }

                    return ComposePart(currentQuery, new AdjacentWordsQueryOperator(tokens));

                default:
                    throw new QueryParserException(ExceptionMessages.UnexpectedTokenEncountered, token.TokenType);
            }
        }

        private IQueryPart CreateWordQueryPart(QueryToken queryToken, ITokenizer tokenizer)
        {
            if (queryToken.TokenType != QueryTokenType.Text)
            {
                throw new QueryParserException(ExceptionMessages.ExpectedTextToken, queryToken.TokenType);
            }

            var tokenText = queryToken.TokenText.AsSpan();

            var isExplicitFuzzyMatch = tokenText.Length > 1 && tokenText[0] == '?';

            if (!isExplicitFuzzyMatch && WildcardQueryPartParser.TryParse(tokenText, tokenizer, out var wildcardQueryPart))
            {
                return wildcardQueryPart;
            }

            IEnumerable<IQueryPart> result;
            if (isExplicitFuzzyMatch || this.options.AssumeFuzzySearchTerms)
            {
                result = tokenizer.Process(isExplicitFuzzyMatch ? tokenText.Slice(1) : tokenText)
                 .Select(token => new FuzzyMatchQueryPart(token.Value));
            }
            else
            {
                result = tokenizer.Process(tokenText)
                     .Select(token => new ExactWordQueryPart(token.Value));
            }

            return ComposeParts(result);
        }

        private static IQueryPart ComposeParts(IEnumerable<IQueryPart> parts)
        {
            IQueryPart? result = null;
            var enumerator = parts.GetEnumerator();

            while (enumerator.MoveNext())
            {
                result = result == null
                    ? enumerator.Current
                    : ComposePart(result, enumerator.Current);
            }

            if (result == null)
            {
                throw new QueryParserException(ExceptionMessages.ExpectedAtLeastOneQueryPartParsed);
            }

            return result;
        }

        private static IQueryPart ComposePart(IQueryPart? existingPart, IQueryPart newPart)
        {
            if (existingPart == null)
            {
                return newPart;
            }

            return CombineParts(existingPart, newPart, QueryTokenType.AndOperator, 0);
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
            switch (tokenType)
            {
                case QueryTokenType.AndOperator:
                    return OperatorPrecedence.And;

                case QueryTokenType.OrOperator:
                    return OperatorPrecedence.Or;

                case QueryTokenType.PrecedingNearOperator:
                case QueryTokenType.NearOperator:
                case QueryTokenType.PrecedingOperator:
                    return OperatorPrecedence.Positional;

                default:
                    throw new QueryParserException(ExceptionMessages.UnexpectedOperatorInternal, tokenType);
            }
        }

        private class QueryParserState
        {
            private readonly IEnumerator<QueryToken> enumerator;

            public QueryParserState(IQueryTokenizer queryTokenizer, string queryText)
            {
                this.enumerator = queryTokenizer.ParseQueryTokens(queryText).GetEnumerator();
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
                if (this.enumerator.MoveNext())
                {
                    return this.enumerator.Current;
                }

                throw new QueryParserException(ExceptionMessages.UnexpectedEndOfQuery);
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
