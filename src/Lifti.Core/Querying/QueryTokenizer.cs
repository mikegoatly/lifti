using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Lifti.Querying
{
    /// <summary>
    /// The default implementation of <see cref="IQueryTokenizer"/>, capable of parsing standard LIFTI query syntax.
    /// </summary>
    internal class QueryTokenizer : IQueryTokenizer
    {
        private static readonly Regex escapeCharacterReplacer = new(@"\\(.)", RegexOptions.Compiled);

        private static readonly HashSet<char> wildcardPunctuation =
        [
            '*',
            '?',
            '%'
        ];

        // Punctuation characters that shouldn't cause a token to be automatically split - these
        // are part of the LIFTI query syntax and processed on a case by case basis.
        private static readonly HashSet<char> generalNonSplitPunctuation = new(wildcardPunctuation)
        {
            '&', // And operator
            '|', // Or operator
            '>', // Preceding operator
            '=', // Field filter
            '(', // Open expression group
            ')', // Close expression group
            '~', // Near operator
            '"', // Quoted sections
            '[', // Field filter open
            ']', // Field filter close
            '^', // Score boost
            '\\' // Escaped characters 
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
            ProcessingNearOperator = 2,
            ProcessingEscapedCharacter = 3,
        }

        private enum FuzzyMatchParseState
        {
            None = 0,
            ProcessingFuzzyMatch = 1,
            ProcessingFuzzyMatchTerm = 2,
        }

        private record QueryTokenizerStackState(int BracketCaptureDepth, IIndexTokenizer IndexTokenizer);

        private record QueryTokenizerState(IIndexTokenizer IndexTokenizer)
        {
            public OperatorParseState OperatorState { get; init; } = OperatorParseState.None;
            public FuzzyMatchParseState FuzzyMatchState { get; init; } = FuzzyMatchParseState.None;
            public int BracketDepth { get; init; }
            public int ScoreBoostStartIndex { get; init; }
            public double? ScoreBoost { get; init; }

            private Stack<QueryTokenizerStackState>? SharedTokenizerStack { get; set; }

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
                this.SharedTokenizerStack ??= new();
                this.SharedTokenizerStack.Push(new QueryTokenizerStackState(this.BracketDepth, this.IndexTokenizer));

                return this with
                {
                    IndexTokenizer = tokenizer
                };
            }

            public QueryTokenizerState UpdateForYieldedToken()
            {
                if (this.OperatorState != OperatorParseState.ProcessingString && this.SharedTokenizerStack?.Count > 0)
                {
                    var peeked = this.SharedTokenizerStack.Peek();
                    if (peeked.BracketCaptureDepth >= this.BracketDepth)
                    {
                        // We've reached the same bracket depth now that the tokenizer was captured at, so revert to the
                        // previous one on the stack.
                        var previousState = this.SharedTokenizerStack.Pop();

                        return this with
                        {
                            IndexTokenizer = previousState.IndexTokenizer,
                            ScoreBoost = null
                        };
                    }
                }

                // Once token processing complete, reset any token state flags
                if (this.FuzzyMatchState != FuzzyMatchParseState.None || this.ScoreBoost != null)
                {
                    return this with
                    {
                        FuzzyMatchState = FuzzyMatchParseState.None,
                        ScoreBoost = null
                    };
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

                    string tokenText;
                    if (state.ScoreBoost == null)
                    {
                        tokenText = queryText.Substring(tokenStart.Value, endIndex - tokenStart.Value);
                    }
                    else
                    {
                        // Don't return the score boost information as part of the token text
                        tokenText = queryText.Substring(tokenStart.Value, state.ScoreBoostStartIndex - tokenStart.Value);
                    }

                    tokenText = StripEscapeIndicators(tokenText);

                    var token = QueryToken.ForText(tokenText, state.IndexTokenizer, state.ScoreBoost);
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
                if (state.FuzzyMatchState == FuzzyMatchParseState.ProcessingFuzzyMatch
                                        && current != ','
                                        && char.IsDigit(current) == false)
                {
                    // As soon as we encounter a non digit or comma when processing a fuzzy match,
                    // assume that we're not processing the fuzzy match parameters - this way a comma can
                    // subsequently be treated as a split character.
                    state = state with { FuzzyMatchState = FuzzyMatchParseState.ProcessingFuzzyMatchTerm };
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
                                case '\\':
                                    tokenStart ??= i;
                                    state = state with { OperatorState = OperatorParseState.ProcessingEscapedCharacter };
                                    break;
                                case '^':
                                    var scoreBoostStart = i;
                                    (var scoreBoost, i) = ConsumeNumber(i + 1, queryText);
                                    state = state with { ScoreBoost = scoreBoost, ScoreBoostStartIndex = scoreBoostStart };
                                    break;
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
                                    switch (state.FuzzyMatchState)
                                    {
                                        case FuzzyMatchParseState.None when tokenStart is null:
                                            // Start processing a fuzzy match operator
                                            state = state with { FuzzyMatchState = FuzzyMatchParseState.ProcessingFuzzyMatch };
                                            break;
                                        case FuzzyMatchParseState.ProcessingFuzzyMatch:
                                            // We're already processing a fuzzy match, the second ? indicates the end of parameters and start of the search term
                                            state = state with { FuzzyMatchState = FuzzyMatchParseState.ProcessingFuzzyMatchTerm };
                                            break;
                                    }

                                    tokenStart ??= i;

                                    break;
                                case '[':
                                    tokenStart = i;

                                    // Keep processing characters until we reach a closing bracket. Characters escaped with a backslash are skipped
                                    var foundCloseBracket = false;
                                    for (i++; i < queryText.Length; i++)
                                    {
                                        current = queryText[i];
                                        if (current == ']')
                                        {
                                            foundCloseBracket = true;
                                            break;
                                        }

                                        if (current == '\\')
                                        {
                                            // Skip the next character
                                            i++;
                                        }
                                    }

                                    if (foundCloseBracket == false)
                                    {
                                        throw new QueryParserException(ExceptionMessages.UnclosedSquareBracket);
                                    }

                                    // Verify that the next character is an =
                                    if (i + 1 == queryText.Length || queryText[i + 1] != '=')
                                    {
                                        throw new QueryParserException(ExceptionMessages.ExpectedEqualsAfterFieldName);
                                    }

                                    break;

                                case '=':
                                    if (tokenStart == null)
                                    {
                                        throw new QueryParserException(ExceptionMessages.UnexpectedOperator, "=");
                                    }

                                    var fieldName = queryText.Substring(tokenStart.Value, i - tokenStart.Value);
                                    if (fieldName.Length > 1 && fieldName[0] == '[')
                                    {
                                        // Strip the square brackets
                                        fieldName = fieldName.Substring(1, fieldName.Length - 2);

                                        // Replace any substituted characters
                                        fieldName = Regex.Replace(fieldName, @"\\(.)", "$1");

                                        if (fieldName.Length == 0)
                                        {
                                            throw new QueryParserException(ExceptionMessages.EmptyFieldNameEncountered);
                                        }
                                    }

                                    yield return QueryToken.ForFieldFilter(fieldName);

                                    state = state.PushTokenizer(tokenizerProvider.GetTokenizerForField(fieldName));

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

                        case OperatorParseState.ProcessingEscapedCharacter:
                            state = state with { OperatorState = OperatorParseState.None };
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

        private static string StripEscapeIndicators(string tokenText)
        {
#if NETSTANDARD
            if (tokenText.IndexOf('\\') >= 0)
#else
            if (tokenText.Contains('\\', StringComparison.Ordinal))
#endif
            {
                return escapeCharacterReplacer.Replace(tokenText, "$1");
            }

            return tokenText;
        }

        private static (double scoreBoost, int endIndex) ConsumeNumber(int index, string queryText)
        {
            var startIndex = index;
            for (; index < queryText.Length; index++)
            {
                var current = queryText[index];
                if (char.IsDigit(current) == false && current != '.')
                {
                    break;
                }
            }

            if (index == startIndex)
            {
                throw new QueryParserException(ExceptionMessages.InvalidScoreBoostExpectedNumber);
            }

            var numberText = queryText.Substring(startIndex, index - startIndex);
            if (double.TryParse(numberText, out var scoreBoost) == false)
            {
                throw new QueryParserException(ExceptionMessages.InvalidScoreBoost, numberText);
            }

            return (scoreBoost, index - 1);
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
                        && !(state.FuzzyMatchState == FuzzyMatchParseState.ProcessingFuzzyMatch && current == ',')), // ..unless it's a comma appearing in the first part of a fuzzy match

                OperatorParseState.ProcessingString => isWhitespace ||
                    (!quotedSectionNonSplitPunctuation.Contains(current) && state.IndexTokenizer.IsSplitCharacter(current)),

                // When processing a near operator or escaped character, no splitting is possible until the operator processing is complete
                OperatorParseState.ProcessingNearOperator or OperatorParseState.ProcessingEscapedCharacter => false,
                _ => throw new QueryParserException(ExceptionMessages.UnexpectedOperatorParseStateEncountered, state.OperatorState)
            };
        }
    }
}
