using Lifti.Querying;
using System.Configuration;
using System.Linq;

namespace Lifti.Tests.Querying
{
    public abstract class QueryTestBase
    {
        protected static CompositeTokenMatchLocation CompositeMatch(int leftWordIndex, int rightWordIndex)
        {
            return new CompositeTokenMatchLocation(TokenMatch(leftWordIndex), TokenMatch(rightWordIndex));
        }

        protected static CompositeTokenMatchLocation CompositeMatch(params int[] wordIndexes)
        {
            var match = CompositeMatch(wordIndexes[0], wordIndexes[1]);

            for (var i = 2; i < wordIndexes.Length; i++)
            {
                match = new CompositeTokenMatchLocation(match, TokenMatch(wordIndexes[i]));
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
                wordIndexes.Select(i => TokenMatch(i)));
        }

        protected static ScoredFieldMatch ScoredFieldMatch(double score, byte fieldId, params (int, int)[] compositeMatches)
        {
            return ScoredFieldMatch(
                score, 
                fieldId,
                compositeMatches.Select(i => (ITokenLocationMatch)CompositeMatch(i.Item1, i.Item2)).ToArray());
        }

        protected static ScoredFieldMatch ScoredFieldMatch(double score, byte fieldId, params ITokenLocationMatch[] compositeMatches)
        {
            return new ScoredFieldMatch(
                score,
                new FieldMatch(fieldId, compositeMatches));
        }

        protected static ITokenLocationMatch TokenMatch(int index)
        {
            return new SingleTokenLocationMatch(new TokenLocation(index, index, (ushort)index));
        }

        protected static ITokenLocationMatch SingleTokenLocationMatch(int index, int start, int length)
        {
            return new SingleTokenLocationMatch(new TokenLocation(index, start, (ushort)length));
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
