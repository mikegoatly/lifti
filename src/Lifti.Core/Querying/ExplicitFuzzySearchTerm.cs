using System;
using System.Text.RegularExpressions;

namespace Lifti.Querying
{
    internal readonly struct ExplicitFuzzySearchTerm
    {
        private static readonly Regex parameterRegex = new Regex(@"^(?<MaxEditDistance>\d*)(,(?<MaxSequentialEdits>\d*))?$", RegexOptions.Compiled);

        internal ExplicitFuzzySearchTerm(bool isFuzzyMatch, int searchTermStartIndex, ushort? maxEditDistance, ushort? maxSequentialEdits)
        {
            this.IsFuzzyMatch = isFuzzyMatch;
            this.SearchTermStartIndex = searchTermStartIndex;
            this.MaxEditDistance = maxEditDistance;
            this.MaxSequentialEdits = maxSequentialEdits;
        }

        public bool IsFuzzyMatch { get; }
        public int SearchTermStartIndex { get; }
        public ushort? MaxEditDistance { get; }
        public ushort? MaxSequentialEdits { get; }

        public static ExplicitFuzzySearchTerm Parse(ReadOnlySpan<char> tokenText)
        {
            var isFuzzyMatch = tokenText.Length > 1 && tokenText[0] == '?';
            var searchTermStartIndex = 1;
            ushort? maxEditDistance = null;
            ushort? maxSequentialEdits = null;

            if (isFuzzyMatch)
            {
                var parameterDelimiter = tokenText.Slice(1).IndexOf('?');
                if (parameterDelimiter != -1)
                {
                    searchTermStartIndex = parameterDelimiter + 2;
                    var parameterText = tokenText.Slice(1, parameterDelimiter).ToString();
                    var parameterMatch = parameterRegex.Match(parameterText);
                    if (!parameterMatch.Success)
                    {
                        throw new QueryParserException(ExceptionMessages.MalformedFuzzyMatchParameters, tokenText.ToString());
                    }


                    if (parameterMatch.Groups["MaxEditDistance"].Length > 0)
                    {
                        if (!ushort.TryParse(parameterMatch.Groups["MaxEditDistance"].Value, out var value))
                        {
                            throw new QueryParserException(ExceptionMessages.FuzzyMatchMaxEditDistanceOutOfRange);
                        }

                        maxEditDistance = value;
                    }

                    if (parameterMatch.Groups["MaxSequentialEdits"].Length > 0)
                    {
                        if (!ushort.TryParse(parameterMatch.Groups["MaxSequentialEdits"].Value, out var value))
                        {
                            throw new QueryParserException(ExceptionMessages.FuzzyMatchMaxSequentialEditsOutOfRange);
                        }

                        maxSequentialEdits = value;
                    }
                }
            }

            return new ExplicitFuzzySearchTerm(isFuzzyMatch, searchTermStartIndex, maxEditDistance, maxSequentialEdits);
        }
    }
}
