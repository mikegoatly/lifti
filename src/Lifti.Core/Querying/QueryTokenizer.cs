using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Lifti.Querying
{
    /// <summary>
    /// The default implementation of <see cref="IQueryTokenizer"/>, capable of parsing standard LIFTI query syntax.
    /// </summary>
    internal class QueryTokenizer : IQueryTokenizer
    {
        private static readonly HashSet<char> wildcardPunctuation = new()
        {
            '*',
            '?',
            '%'
        };

        // Punctuation characters that shouldn't cause a token to be automatically split - these
        // are part of the LIFTI query syntax and processed on a case by case basis.
        private static readonly HashSet<char> generalNonSplitPunctuation = new(wildcardPunctuation)
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
        private static readonly HashSet<char> quotedSectionNonSplitPunctuation = new(wildcardPunctuation)
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

        private record IndexTokenizerStackState(int BracketCaptureDepth, IIndexTokenizer IndexTokenizer);

        private record QueryTokenizerState(IIndexTokenizer IndexTokenizer)
        {
            public OperatorParseState OperatorState { get; init; } = OperatorParseState.None;
            public TokenParseState TokenState { get; init; } = TokenParseState.None;
            public int BracketDepth { get; init; }

            private ImmutableStack<IndexTokenizerStackState> TokenizerStack { get; init; } = ImmutableStack<IndexTokenizerStackState>.Empty;

            public QueryTokenizerState OpenBracket()
            {
                return this with
                {
                    BracketDepth = this.BracketDepth + 1
                };
            }

            public QueryTokenizerState CloseBracket()
            {
                return this.BracketDepth > 0
                    ? (this with
                    {
                        BracketDepth = this.BracketDepth - 1
                    })
                    : throw new QueryParserException(ExceptionMessages.UnexpectedCloseBracket);
            }

            public QueryTokenizerState PushTokenizer(IIndexTokenizer tokenizer)
            {
                return this with
                {
                    IndexTokenizer = tokenizer,
                    TokenizerStack = this.TokenizerStack.Push(new IndexTokenizerStackState(this.BracketDepth, this.IndexTokenizer))
                };
            }

            public QueryTokenizerState UpdateForYieldedToken()
            {
                if (this.OperatorState != OperatorParseState.ProcessingString && this.TokenizerStack.IsEmpty == false)
                {
                    var peeked = this.TokenizerStack.Peek();
                    if (peeked.BracketCaptureDepth >= this.BracketDepth)
                    {
                        // We've reached the same bracket depth now that the tokenizer was captured at, so revert to the
                        // previous one on the stack.
                        var poppedStack = this.TokenizerStack.Pop(out var previousState);

                        return this with
                        {
                            IndexTokenizer = previousState.IndexTokenizer,
                            TokenizerStack = poppedStack
                        };
                    }
                }

                // Once token processing complete, reset any token state flags
                if (this.TokenState != TokenParseState.None)
                {
                    return this with { TokenState = TokenParseState.None };
                }

                // We're still deeper in brackets than when we first captured the tokenizer, so don't revert
                return this;
            }
        }

        /// <inheritdoc />
        public IEnumerable<QueryToken> ParseQueryTokens(string queryText, IIndexTokenizerProvider tokenizerProvider)
        {
            if (queryText is null)
            {
                throw new ArgumentNullException(nameof(queryText));
            }

            var state = new QueryTokenizerState(tokenizerProvider.DefaultTokenizer);
            var tokenStart = (int?)null;
            var tolerance = 0;

            QueryToken? CreateTokenForYielding(int endIndex)
            {
                if (tokenStart != null)
                {
                    var tokenText = queryText.Substring(tokenStart.Value, endIndex - tokenStart.Value);
                    var token = QueryToken.ForText(tokenText, state.IndexTokenizer);
                    tokenStart = null;

                    state = state.UpdateForYieldedToken();

                    if (tokenText.Length == 0 || (tokenText[0] == '?' && tokenText[tokenText.Length - 1] == '?'))
                    {
                        // This is an edge case where we have a fuzzy search without any search term. It could be either
                        // just a "?" or a fuzzy search with parameters but no search term, e.g. "?4,1?"
                        // This should be considered the same as an empty search term, so don't return a token
                        return null;
                    }

                    return token;
                }

                return null;
            }

            QueryToken? token;
            for (var i = 0; i < queryText.Length; i++)
            {
                var current = queryText[i];
                if (state.TokenState == TokenParseState.ProcessingFuzzyMatch
                                        && current != ','
                                        && char.IsDigit(current) == false)
                {
                    // As soon as we encounter a non digit or comma when processing a fuzzy match,
                    // assume that we're not processing the fuzzy match parameters - this way a comma can
                    // subsequently be treated as a split character.
                    state = state with { TokenState = TokenParseState.ProcessingFuzzyMatchTerm };
                }

                if (IsSplitChar(current, state))
                {
                    token = CreateTokenForYielding(i);
                    if (token is not null)
                    {
                        yield return token;
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

                                case '=':
                                    if (tokenStart == null)
                                    {
                                        throw new QueryParserException(ExceptionMessages.UnexpectedOperator, "=");
                                    }

                                    var fieldName = queryText.Substring(tokenStart.Value, i - tokenStart.Value);
                                    yield return QueryToken.ForFieldFilter(fieldName);

                                    state = state.PushTokenizer(tokenizerProvider[fieldName]);

                                    tokenStart = null;
                                    break;
                                case ')':
                                    state = state.CloseBracket();
                                    token = CreateTokenForYielding(i);
                                    if (token is not null)
                                    {
                                        yield return token;
                                    }

                                    yield return QueryToken.ForOperator(QueryTokenType.CloseBracket);
                                    break;
                                case '(':
                                    yield return QueryToken.ForOperator(QueryTokenType.OpenBracket);
                                    state = state.OpenBracket();
                                    break;
                                case '~':
                                    tolerance = 0;
                                    state = state with { OperatorState = OperatorParseState.ProcessingNearOperator };
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
                                    if (token is not null)
                                    {
                                        yield return token;
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
                if (token is not null)
                {
                    yield return token;
                }
            }
        }

        private static bool IsSplitChar(char current, QueryTokenizerState state)
        {
            var isWhitespace = char.IsWhiteSpace(current);
            return state.OperatorState switch
            {
                OperatorParseState.None =>
                    isWhitespace || // Whitespace is always a split character
                    (!generalNonSplitPunctuation.Contains(current)
                        && state.IndexTokenizer.IsSplitCharacter(current) // Defer to the tokenizer for the field as to whether this is a split character,
                        && !(state.TokenState == TokenParseState.ProcessingFuzzyMatch && current == ',')), // ..unless it's a comma appearing in the first part of a fuzzy match

                OperatorParseState.ProcessingString => isWhitespace ||
                    (!quotedSectionNonSplitPunctuation.Contains(current) && state.IndexTokenizer.IsSplitCharacter(current)),

                // When processing a near operator, no splitting is possible until the operator processing is complete
                OperatorParseState.ProcessingNearOperator => false,
                _ => throw new QueryParserException(ExceptionMessages.UnexpectedOperatorParseStateEncountered, state.OperatorState)
            };
        }
    }
}
