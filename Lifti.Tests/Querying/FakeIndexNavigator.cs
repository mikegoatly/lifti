using Lifti.Querying;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tests.Querying
{
    public class FakeIndexNavigator : IIndexNavigator
    {
        public FakeIndexNavigator()
        {
        }

        private FakeIndexNavigator(bool exactAndChildMatchOnly, params int[] matchedItems)
        {
            this.ExpectedExactAndChildMatches = new IntermediateQueryResult(
                matchedItems.Select(
                    m => new QueryWordMatch(m, new[] { new FieldMatch((byte)m, new[] { new WordLocation(m, m, m) }) })));

            this.ExpectedExactMatches = exactAndChildMatchOnly ? IntermediateQueryResult.Empty : this.ExpectedExactAndChildMatches;
        }

        public IntermediateQueryResult ExpectedExactMatches { get; set; }
        public IntermediateQueryResult ExpectedExactAndChildMatches { get; set; }
        public List<char> NavigatedCharacters { get; } = new List<char>();
        public List<string> NavigatedStrings { get; } = new List<string>();

        public static FakeIndexNavigator ReturningExactMatches(params int[] matchedItems)
        {
            return new FakeIndexNavigator(false, matchedItems);
        }

        public static FakeIndexNavigator ReturningExactAndChildMatches(params int[] matchedItems)
        {
            return new FakeIndexNavigator(true, matchedItems);
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
    }
}
