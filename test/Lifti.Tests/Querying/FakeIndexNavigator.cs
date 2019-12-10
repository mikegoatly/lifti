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

        private FakeIndexNavigator(bool exactAndChildMatchOnly, params int[] matchedItems)
        {
            this.ExpectedExactAndChildMatches = new IntermediateQueryResult(
                matchedItems.Select(
                    m => new QueryWordMatch(m, new[] { FieldMatch((byte)m, m) })));

            this.ExpectedExactMatches = exactAndChildMatchOnly ? Lifti.Querying.IntermediateQueryResult.Empty : this.ExpectedExactAndChildMatches;
        }

        private FakeIndexNavigator(bool exactAndChildMatchOnly, params QueryWordMatch[] matches)
        {
            this.ExpectedExactAndChildMatches = new IntermediateQueryResult(matches);

            this.ExpectedExactMatches = exactAndChildMatchOnly ? Lifti.Querying.IntermediateQueryResult.Empty : this.ExpectedExactAndChildMatches;
        }

        public IntermediateQueryResult ExpectedExactMatches { get; set; }
        public IntermediateQueryResult ExpectedExactAndChildMatches { get; set; }
        public List<char> NavigatedCharacters { get; } = new List<char>();
        public List<string> NavigatedStrings { get; } = new List<string>();

        public bool HasExactMatches => this.ExpectedExactMatches.Matches.Count > 0;

        public static FakeIndexNavigator ReturningExactMatches(params int[] matchedItems)
        {
            return new FakeIndexNavigator(false, matchedItems);
        }

        public static FakeIndexNavigator ReturningExactAndChildMatches(params int[] matchedItems)
        {
            return new FakeIndexNavigator(true, matchedItems);
        }

        public static FakeIndexNavigator ReturningExactMatches(params QueryWordMatch[] matches)
        {
            return new FakeIndexNavigator(false, matches);
        }

        public static FakeIndexNavigator ReturningExactAndChildMatches(params QueryWordMatch[] matches)
        {
            return new FakeIndexNavigator(true, matches);
        }

        public IntermediateQueryResult GetExactAndChildMatches()
        {
            return this.ExpectedExactAndChildMatches;
        }

        public IntermediateQueryResult GetExactMatches()
        {
            return this.ExpectedExactMatches;
        }

        public bool Process(char value)
        {
            this.NavigatedCharacters.Add(value);
            return true;
        }

        public bool Process(ReadOnlySpan<char> text)
        {
            this.NavigatedStrings.Add(new string(text));
            return true;
        }

        public IEnumerable<string> EnumerateIndexedWords()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}
