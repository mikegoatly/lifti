using Lifti.Querying.QueryParts;
using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public class QueryParser : IQueryParser
    {
        public IQuery Parse(IIndexedFieldLookup fieldLookup, string queryText, ITokenizer wordTokenizer)
        {
            if (fieldLookup is null)
            {
                throw new ArgumentNullException(nameof(fieldLookup));
            }

            IQueryPart rootPart = null;

            var state = new QueryParserState(queryText);
            while (state.TryGetNextToken(out var token))
            {
                rootPart = CreateQueryPart(fieldLookup, state, token, wordTokenizer, rootPart);
            }

            return new Query(rootPart);
        }

        private static IQueryPart CreateQueryPart(
            IIndexedFieldLookup fieldLookup, 
            QueryParserState state, 
            QueryToken token, 
            ITokenizer wordTokenizer, 
            IQueryPart rootPart)
        {
            switch (token.TokenType)
            {
                case QueryTokenType.Text:
                    return ComposePart(rootPart, CreateWordPart(token, wordTokenizer));

                case QueryTokenType.FieldFilter:
                    var filteredPart = CreateQueryPart(fieldLookup, state, state.GetNextToken(), wordTokenizer, null);
                    if (fieldLookup.TryGetIdForField(token.TokenText, out var fieldId))
                    {
                        return ComposePart(
                            rootPart,
                            new FieldFilterQueryOperator(token.TokenText, fieldId, filteredPart));
                    }

                    throw new QueryParserException(ExceptionMessages.UnknownFieldReference, token.TokenText);

                case QueryTokenType.OrOperator:
                case QueryTokenType.AndOperator:
                case QueryTokenType.NearOperator:
                case QueryTokenType.PrecedingNearOperator:
                case QueryTokenType.PrecedingOperator:
                    var rightPart = CreateQueryPart(fieldLookup, state, state.GetNextToken(), wordTokenizer, null);
                    return CombineParts(rootPart, rightPart, token.TokenType, token.Tolerance);

                case QueryTokenType.OpenBracket:
                    var bracketedPart = state.GetTokensUntil(QueryTokenType.CloseBracket)
                        .Aggregate((IQueryPart)null, (current, next) => CreateQueryPart(fieldLookup, state, next, wordTokenizer, current));

                    return bracketedPart == null
                               ? rootPart
                               : ComposePart(rootPart, new BracketedQueryPart(bracketedPart));

                default:
                    throw new QueryParserException(ExceptionMessages.UnexpectedTokenEncountered, token.TokenType);
            }
        }

        private static IWordQueryPart CreateWordPart(QueryToken queryToken, ITokenizer wordTokenizer)
        {
            var tokenText = queryToken.TokenText.AsSpan();

            var hasWildcard = tokenText.Length > 0 && tokenText[tokenText.Length - 1] == '*';
            if (hasWildcard)
            {
                tokenText = tokenText.Slice(0, tokenText.Length - 1);
            }

            var tokenizedWord = wordTokenizer.Process(tokenText).Single();
            
            return hasWildcard ? (IWordQueryPart)new StartsWithWordQueryPart(tokenizedWord.Value) : new ExactWordQueryPart(tokenizedWord.Value);
        }

        private static IQueryPart ComposePart(IQueryPart existingPart, IQueryPart newPart)
        {
            if (existingPart == null)
            {
                return newPart;
            }

            return CombineParts(existingPart, newPart, QueryTokenType.AndOperator, 0);
        }

        private static IBinaryQueryOperator CombineParts(IQueryPart existingPart, IQueryPart newPart, QueryTokenType operatorType, int tolerance)
        {
            if (existingPart == null)
            {
                throw new QueryParserException(ExceptionMessages.UnexpectedOperator, operatorType);
            }

            var existingBinaryOperator = existingPart as IBinaryQueryOperator;
            if (existingBinaryOperator != null)
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
            switch (tokenType)
            {
                case QueryTokenType.AndOperator:
                    return new AndQueryOperator(leftPart, rightPart);

                case QueryTokenType.OrOperator:
                    return new OrQueryOperator(leftPart, rightPart);

                case QueryTokenType.NearOperator:
                    return new NearQueryOperator(leftPart, rightPart, tolerance);

                case QueryTokenType.PrecedingNearOperator:
                    return new PrecedingNearQueryOperator(leftPart, rightPart, tolerance);

                case QueryTokenType.PrecedingOperator:
                    return new PrecedingQueryOperator(leftPart, rightPart);

                default:
                    throw new QueryParserException(ExceptionMessages.UnexpectedOperatorInternal, tokenType);
            }
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

            public QueryParserState(string queryText)
            {
                this.enumerator = new QueryTokenizer().ParseQueryTokens(queryText).GetEnumerator();
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
