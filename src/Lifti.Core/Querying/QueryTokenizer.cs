using System;
using System.Collections.Generic;

namespace Lifti.Querying
{
    /// <summary>
    /// The default implementation of <see cref="IQueryTokenizer"/>, capable of parsing standard LIFTI query syntax.
    /// </summary>
    internal class QueryTokenizer : IQueryTokenizer
    {
        private static readonly HashSet<char> wildcardPunctuation = new HashSet<char>
        {
            '*',
            '?',
            '%',
            ',' // A comma is used in the definition of a fuzzy match operator. It will be treated as a separator if encountered anywhere else.
        };

        // Punctuation characters that shouldn't cause a token to be automatically split - these
        // are part of the LIFTI query syntax and processed on a case by case basis.
        private static readonly HashSet<char> generalNonSplitPunctuation = new HashSet<char>(wildcardPunctuation)
        {
            '&',
            '|',
            '>',
            '=',
            '(',
            ')',
            '~',
            '"'
        };

        // Punctuation characters that shouldn't cause a token to be automatically split when processing
        // inside a quoted section
        private static readonly HashSet<char> quotedSectionNonSplitPunctuation = new HashSet<char>(wildcardPunctuation)
        {
            '"'
        };

        private enum OperatorParseState
        {
            None = 0,
            ProcessingString = 1,
            ProcessingNearOperator = 2
        }

        private enum TokenParseState
        {
            None = 0,
            ProcessingFuzzyMatch = 1,
            ProcessingFuzzyMatchTerm = 2,
        }

        private record QueryTokenizerState(OperatorParseState OperatorState = OperatorParseState.None, TokenParseState TokenState = TokenParseState.None);

        /// <inheritdoc />
        public IEnumerable<QueryToken> ParseQueryTokens(string queryText)
        {
            if (queryText is null)
            {
                throw new ArgumentNullException(nameof(queryText));
            }

            var state = new QueryTokenizerState();
            var tokenStart = (int?)null;
            var tolerance = 0;

            QueryToken? CreateTokenForYielding(int endIndex)
            {
                if (tokenStart != null)
                {
                    var tokenText = queryText.Substring(tokenStart.Value, endIndex - tokenStart.Value);
                    var token = QueryToken.ForText(tokenText);
                    tokenStart = null;

                    // Once token processing complete, reset any token state flags
                    if (state.TokenState != TokenParseState.None)
                    {
                        state = state with { TokenState = TokenParseState.None };
                    }

                    return token;
                }

                return null;
            }

            QueryToken? token;
            for (var i = 0; i < queryText.Length; i++)
            {
                var current = queryText[i];
                if (IsSplitChar(current, state))
                {
                    token = CreateTokenForYielding(i);
                    if (token != null)
                    {
                        yield return token.Value;
                    }
                }
                else
                {
                    switch (state.OperatorState)
                    {
                        case OperatorParseState.None:
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
                                case '?':
                                    // Possibly a wildcard token character, or part of a fuzzy match token
                                    switch (state.TokenState)
                                    {
                                        case TokenParseState.None when tokenStart is null:
                                            // Start processing a fuzzy match operator
                                            state = state with { TokenState = TokenParseState.ProcessingFuzzyMatch };
                                            break;
                                        case TokenParseState.ProcessingFuzzyMatch:
                                            // We're already procssing a fuzzy match, the second ? indicates the end of parameters and start of the search term
                                            state = state with { TokenState = TokenParseState.ProcessingFuzzyMatchTerm };
                                            break;
                                    }

                                    tokenStart ??= i;

                                    break;

                                case ',':
                                    // Commas should only appear in the parameters of a fuzzy search token
                                    if (state.TokenState != TokenParseState.ProcessingFuzzyMatch)
                                    {
                                        token = CreateTokenForYielding(i);
                                        if (token != null)
                                        {
                                            yield return token.Value;
                                        }
                                    }

                                    break;

                                case '=':
                                    if (tokenStart == null)
                                    {
                                        throw new QueryParserException(ExceptionMessages.UnexpectedOperator, "=");
                                    }

                                    yield return QueryToken.ForFieldFilter(queryText.Substring(tokenStart.Value, i - tokenStart.Value));
                                    tokenStart = null;
                                    break;
                                case ')':
                                    token = CreateTokenForYielding(i);
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
                                    state = state with { OperatorState = OperatorParseState.ProcessingNearOperator};
                                    break;
                                case '"':
                                    state = state with { OperatorState = OperatorParseState.ProcessingString };
                                    yield return QueryToken.ForOperator(QueryTokenType.BeginAdjacentTextOperator);
                                    break;
                                default:
                                    tokenStart ??= i;
                                    break;
                            }

                            break;

                        case OperatorParseState.ProcessingString:
                            switch (current)
                            {
                                case '"':
                                    state = state with { OperatorState = OperatorParseState.None };
                                    token = CreateTokenForYielding(i);
                                    if (token != null)
                                    {
                                        yield return token.Value;
                                    }

                                    yield return QueryToken.ForOperator(QueryTokenType.EndAdjacentTextOperator);
                                    break;
                                default:
                                    tokenStart ??= i;
                                    break;
                            }

                            break;

                        case OperatorParseState.ProcessingNearOperator:
                            switch (current)
                            {
                                case '>':
                                    yield return QueryToken.ForOperatorWithTolerance(QueryTokenType.PrecedingNearOperator, tolerance);
                                    state = state with { OperatorState = OperatorParseState.None };
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
                                    state = state with { OperatorState = OperatorParseState.None };
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
                token = CreateTokenForYielding(queryText.Length);
                if (token != null)
                {
                    yield return token.GetValueOrDefault();
                }
            }
        }

        private static bool IsSplitChar(char current, QueryTokenizerState state)
        {
            var isWhitespace = char.IsWhiteSpace(current);
            return state.OperatorState switch
            {
                OperatorParseState.None => isWhitespace ||
                    (!generalNonSplitPunctuation.Contains(current) && char.IsPunctuation(current)),

                OperatorParseState.ProcessingString => isWhitespace ||
                    (!quotedSectionNonSplitPunctuation.Contains(current) && char.IsPunctuation(current)),

                // When processing a near operator, no splitting is possible until the operator processing is complete
                OperatorParseState.ProcessingNearOperator => false
            };
        }
    }
}
