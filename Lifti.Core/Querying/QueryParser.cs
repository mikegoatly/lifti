using System;
using System.Collections.Generic;
using System.Linq;
using Lifti.Tokenization;

namespace Lifti.Querying
{
    public class QueryParser : IQueryParser
    {
        public IQuery Parse(string queryText, ITokenizer wordTokenizer)
        {
            IQueryPart rootPart = null;

            var state = new QueryParserState(queryText);
            QueryToken token;
            while (state.TryGetNextToken(out token))
            {
                rootPart = CreateQueryPart(state, token, wordTokenizer, rootPart);
            }

            return new Query(rootPart);
        }

        private static IQueryPart CreateQueryPart(QueryParserState state, QueryToken token, ITokenizer wordTokenizer, IQueryPart rootPart)
        {
            switch (token.TokenType)
            {
                case QueryTokenType.Text:
                    return CreateWordParts(token, wordTokenizer).Aggregate(rootPart, ComposePart);

                case QueryTokenType.AndOperator:
                    var rightPart = CreateQueryPart(state, state.GetNextToken(), wordTokenizer, null);
                    return CombineParts(rootPart, rightPart, token.TokenType);

                default:
                    throw new QueryParserException(ExceptionMessages.UnexpectedTokenEncountered, token.TokenType);
            }
        }

        private static IEnumerable<IWordQueryPart> CreateWordParts(QueryToken token, ITokenizer wordTokenizer)
        {
            return from w in wordTokenizer.Process(token.TokenText.AsSpan())
                   let word = w.Value
                   select new ExactWordQueryPart(word);
        }

        private static IQueryPart ComposePart(IQueryPart existingPart, IQueryPart newPart)
        {
            if (existingPart == null)
            {
                return newPart;
            }

            return CombineParts(existingPart, newPart, QueryTokenType.AndOperator);
        }

        private static IBinaryQueryOperator CombineParts(IQueryPart existingPart, IQueryPart newPart, QueryTokenType operatorType)
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
                    existingBinaryOperator.Right = CreateOperator(operatorType, existingBinaryOperator.Right, newPart);
                    return existingBinaryOperator;
                }

                return CreateOperator(operatorType, existingBinaryOperator, newPart);
            }

            return CreateOperator(operatorType, existingPart, newPart);
        }

        private static IBinaryQueryOperator CreateOperator(QueryTokenType tokenType, IQueryPart leftPart, IQueryPart rightPart)
        {
            switch (tokenType)
            {
                case QueryTokenType.AndOperator:
                    return new AndQueryOperator(leftPart, rightPart);

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
                    return OperatorPrecedence.Locational;

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

    public class Query : IQuery
    {
        private readonly IQueryPart root;

        public Query(IQueryPart root)
        {
            this.root = root;
        }

        public IEnumerable<SearchResult<TKey>> Execute<TKey>(IFullTextIndex<TKey> index)
        {
            var matches = root.Evaluate(() => new IndexNavigator(index.Root)).Matches;
            var results = new Dictionary<int, List<IndexedWordLocation>>();

            foreach (var (itemId, indexedWordLocations) in matches)
            {
                if (!results.TryGetValue(itemId, out var itemResults))
                {
                    itemResults = new List<IndexedWordLocation>();
                    results[itemId] = itemResults;
                }

                itemResults.AddRange(indexedWordLocations);
            }

            foreach (var itemResults in matches)
            {
                var item = index.IdPool.GetItemForId(itemResults.itemId);
                yield return new SearchResult<TKey>(
                    item,
                    itemResults.indexedWordLocations.Select(m => new MatchedLocation(index.FieldLookup.GetFieldForId(m.FieldId), m.Locations)).ToList());
            }
        }
    }

}
