using Lifti.Querying;
using System.Configuration;
using System.Linq;

namespace Lifti.Tests.Querying
{
    public abstract class QueryTestBase
    {
        protected static CompositeWordMatchLocation CompositeMatch(int leftWordIndex, int rightWordIndex)
        {
            return new CompositeWordMatchLocation(WordMatch(leftWordIndex), WordMatch(rightWordIndex));
        }

        protected static CompositeWordMatchLocation CompositeMatch(params int[] wordIndexes)
        {
            var match = CompositeMatch(wordIndexes[0], wordIndexes[1]);

            for (var i = 2; i < wordIndexes.Length; i++)
            {
                match = new CompositeWordMatchLocation(match, WordMatch(wordIndexes[i]));
            }

            return match;
        }

        protected static FieldMatch FieldMatch(byte fieldId, params int[] wordIndexes)
        {
            return new FieldMatch(
                    fieldId,
                    wordIndexes.Select(i => WordMatch(i)).ToList());
        }

        protected static FieldMatch FieldMatch(byte fieldId, params (int, int)[] compositeMatches)
        {
            return new FieldMatch(
                    fieldId,
                    compositeMatches.Select(i => (IWordLocationMatch)CompositeMatch(i.Item1, i.Item2)).ToList());
        }

        protected static IWordLocationMatch WordMatch(int index)
        {
            return new SingleWordLocationMatch(new WordLocation(index, index, index));
        }

        protected static IntermediateQueryResult IntermediateQueryResult(params QueryWordMatch[] matches)
        {
            return new IntermediateQueryResult(matches);
        }

        protected static QueryWordMatch QueryWordMatch(int itemId, params FieldMatch[] matches)
        {
            return new QueryWordMatch(itemId, matches);
        }
    }
}
