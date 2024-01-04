using Lifti.Querying;
using System.Collections.Generic;
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
            return Lifti.Querying.ScoredFieldMatch.CreateFromUnsorted(
                    score,
                    fieldId,
                    TokenLocationMatches(wordIndexes));
        }

        protected static List<ITokenLocationMatch> TokenLocationMatches(params int[] wordIndexes)
        {
            return wordIndexes.Select(TokenMatch).ToList();
        }

        protected static List<TokenLocation> TokenLocations(params int[] wordIndexes)
        {
            return wordIndexes.Select(x => new TokenLocation(x, x, (ushort)x)).ToList();
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
            return Lifti.Querying.ScoredFieldMatch.CreateFromUnsorted(
                score,
                fieldId, 
                compositeMatches.ToList());
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
