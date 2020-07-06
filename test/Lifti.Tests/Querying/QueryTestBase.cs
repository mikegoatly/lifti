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

        protected static ScoredFieldMatch ScoredFieldMatch(double score, byte fieldId, params int[] wordIndexes)
        {
            return new ScoredFieldMatch(
                    score,
                    FieldMatch(fieldId, wordIndexes));
        }

        protected static FieldMatch FieldMatch(byte fieldId, params int[] wordIndexes)
        {
            return new FieldMatch(
                    fieldId,
                    wordIndexes.Select(i => WordMatch(i)).ToList());
        }

        protected static ScoredFieldMatch ScoredFieldMatch(double score, byte fieldId, params (int, int)[] compositeMatches)
        {
            return ScoredFieldMatch(
                score, 
                fieldId,
                compositeMatches.Select(i => (IWordLocationMatch)CompositeMatch(i.Item1, i.Item2)).ToArray());
        }

        protected static ScoredFieldMatch ScoredFieldMatch(double score, byte fieldId, params IWordLocationMatch[] compositeMatches)
        {
            return new ScoredFieldMatch(
                score,
                new FieldMatch(fieldId, compositeMatches));
        }

        protected static IWordLocationMatch WordMatch(int index)
        {
            return new SingleWordLocationMatch(new WordLocation(index, index, (ushort)index));
        }

        protected static IWordLocationMatch WordMatch(int index, int start, int length)
        {
            return new SingleWordLocationMatch(new WordLocation(index, start, (ushort)length));
        }

        protected static IntermediateQueryResult IntermediateQueryResult(params ScoredToken[] matches)
        {
            return new IntermediateQueryResult(matches);
        }

        protected static ScoredToken ScoredToken(int itemId, params ScoredFieldMatch[] matches)
        {
            return new ScoredToken(
                itemId, 
                matches);
        }
    }
}
