using Lifti.Querying;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tests.Querying
{
    public class FakeIndexNavigator : QueryTestBase, IIndexNavigator
    {
        public FakeIndexNavigator()
        {
        }

        private FakeIndexNavigator(bool exactAndChildMatchOnly, params int[] matchedDocumentIds)
        {
            this.ExpectedExactAndChildMatches = new IntermediateQueryResult(
                matchedDocumentIds.Select(
                    m => ScoredToken(
                        m,
                        [ScoredFieldMatch(1D, (byte)m, m)])));

            this.ExpectedExactMatches = exactAndChildMatchOnly ? Lifti.Querying.IntermediateQueryResult.Empty : this.ExpectedExactAndChildMatches;
        }

        private FakeIndexNavigator(bool exactAndChildMatchOnly, params ScoredToken[] matches)
        {
            this.ExpectedExactAndChildMatches = new IntermediateQueryResult(matches);

            this.ExpectedExactMatches = exactAndChildMatchOnly ? Lifti.Querying.IntermediateQueryResult.Empty : this.ExpectedExactAndChildMatches;
        }

        public IntermediateQueryResult ExpectedExactMatches { get; set; }
        public IntermediateQueryResult ExpectedExactAndChildMatches { get; set; }
        public List<char> NavigatedCharacters { get; } = [];
        public List<string> NavigatedStrings { get; } = [];
        public List<double> ProvidedWeightings { get; } = [];

        public bool HasExactMatches => this.ExpectedExactMatches.Matches.Count > 0;

        public static FakeIndexNavigator ReturningExactMatches(params int[] matchedDocumentIds)
        {
            return new FakeIndexNavigator(false, matchedDocumentIds);
        }

        public static FakeIndexNavigator ReturningExactAndChildMatches(params int[] matchedDocumentIds)
        {
            return new FakeIndexNavigator(true, matchedDocumentIds);
        }

        public static FakeIndexNavigator ReturningExactMatches(params ScoredToken[] matches)
        {
            return new FakeIndexNavigator(false, matches);
        }

        public static FakeIndexNavigator ReturningExactAndChildMatches(params ScoredToken[] matches)
        {
            return new FakeIndexNavigator(true, matches);
        }

        public IntermediateQueryResult GetExactAndChildMatches(double weighting = 1D)
        {
            this.ProvidedWeightings.Add(weighting);
            return this.ExpectedExactAndChildMatches;
        }

        public IntermediateQueryResult GetExactMatches(double weighting = 1D)
        {
            this.ProvidedWeightings.Add(weighting);
            return this.ExpectedExactMatches;
        }

        public void AddExactAndChildMatches(MatchCollector matchCollector)
        {
            throw new NotImplementedException();
        }

        public void AddExactMatches(MatchCollector matchCollector)
        {
            throw new NotImplementedException();
        }

        public IntermediateQueryResult CreateIntermediateQueryResult(MatchCollector matches, double weighting = 1)
        {
            throw new NotImplementedException();
        }

        public bool Process(char value)
        {
            this.NavigatedCharacters.Add(value);
            return true;
        }

        public bool Process(string text)
        {
            this.NavigatedStrings.Add(text);
            return true;
        }

        public bool Process(ReadOnlySpan<char> text)
        {
            this.NavigatedStrings.Add(text.ToString());
            return true;
        }

        public IEnumerable<string> EnumerateIndexedTokens()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public IIndexNavigatorBookmark CreateBookmark()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<char> EnumerateNextCharacters()
        {
            throw new NotImplementedException();
        }
    }
}
