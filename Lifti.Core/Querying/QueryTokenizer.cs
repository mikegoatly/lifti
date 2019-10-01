using System;
using System.Collections.Generic;

namespace Lifti.Querying
{
    public class QueryTokenizer : IQueryTokenizer
    {
        private enum State
        {
            None = 0,
            ProcessingString = 1,
            ProcessingNearOperator = 2
        }

        public IEnumerable<QueryToken> ParseQueryTokens(string queryText)
        {
            if (queryText is null)
            {
                throw new ArgumentNullException(nameof(queryText));
            }

            var state = State.None;
            var tokenStart = (int?)null;
            var tolerance = 0;

            QueryToken? createWordForYielding(int endIndex)
            {
                if (tokenStart != null)
                {
                    var token = QueryToken.ForWord(queryText.Substring(tokenStart.Value, endIndex - tokenStart.Value));
                    tokenStart = null;
                    return token;
                }

                return null;
            }

            for (var i = 0; i < queryText.Length; i++)
            {
                var current = queryText[i];
                if (char.IsWhiteSpace(current))
                {
                    var token = createWordForYielding(i);
                    if (token != null)
                    {
                        yield return token.Value;
                    }
                }
                else
                {
                    switch (state)
                    {
                        case State.None:
                            switch (current)
                            {
                                case '&':
                                    yield return QueryToken.ForOperator(QueryTokenType.AndOperator);
                                    break;
                                case '|':
                                    yield return QueryToken.ForOperator(QueryTokenType.OrOperator);
                                    break;
                                case '>':
                                    yield return QueryToken.ForOperator(QueryTokenType.PrecedingOperator);
                                    break;
                                case ')':
                                    var token = createWordForYielding(i);
                                    if (token != null)
                                    {
                                        yield return token.Value;
                                    }

                                    yield return QueryToken.ForOperator(QueryTokenType.CloseBracket);
                                    break;
                                case '(':
                                    yield return QueryToken.ForOperator(QueryTokenType.OpenBracket);
                                    break;
                                case '~':
                                    tolerance = 0;
                                    state = State.ProcessingNearOperator;
                                    break;
                                case '"':
                                    state = State.ProcessingString;
                                    yield return QueryToken.ForOperator(QueryTokenType.BeginAdjacentTextOperator);
                                    break;
                                default:
                                    tokenStart = tokenStart ?? i;
                                    break;
                            }

                            break;
                        case State.ProcessingString:
                            switch (current)
                            {
                                case '"':
                                    state = State.None;
                                    var token = createWordForYielding(i);
                                    if (token != null)
                                    {
                                        yield return token.Value;
                                    }

                                    yield return QueryToken.ForOperator(QueryTokenType.EndAdjacentTextOperator);
                                    break;
                                default:
                                    tokenStart = tokenStart ?? i;
                                    break;
                            }

                            break;

                        case State.ProcessingNearOperator:
                            switch (current)
                            {
                                case '>':
                                    yield return QueryToken.ForOperatorWithTolerance(QueryTokenType.PrecedingNearOperator, tolerance);
                                    state = State.None;
                                    break;

                                case '0':
                                case '1':
                                case '2':
                                case '3':
                                case '4':
                                case '5':
                                case '6':
                                case '7':
                                case '8':
                                case '9':
                                    tolerance = (tolerance * 10) + (current - '0');
                                    break;
                                default:
                                    yield return QueryToken.ForOperatorWithTolerance(QueryTokenType.NearOperator, tolerance);
                                    state = State.None;
                                    // Skip back a character to re-process it now state is None
                                    i -= 1;
                                    break;
                            }

                            break;
                    }
                }
            }

            if (tokenStart != null)
            {
                yield return QueryToken.ForWord(queryText.Substring(tokenStart.Value, queryText.Length - tokenStart.Value));
            }
        }
    }
}
