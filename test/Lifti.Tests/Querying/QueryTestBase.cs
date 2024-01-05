using Lifti.Querying;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tests.Querying
{
    public abstract class QueryTestBase
    {
        internal static CompositeTokenLocation CompositeMatch(int leftWordIndex, int rightWordIndex)
        {
            return new CompositeTokenLocation(TokenMatch(leftWordIndex), TokenMatch(rightWordIndex));
        }

        internal static CompositeTokenLocation CompositeMatch(params int[] wordIndexes)
        {
            var match = CompositeMatch(wordIndexes[0], wordIndexes[1]);

            for (var i = 2; i < wordIndexes.Length; i++)
            {
                match = new CompositeTokenLocation(match, TokenMatch(wordIndexes[i]));
            }

            return match;
        }

        internal static ScoredFieldMatch ScoredFieldMatch(double score, byte fieldId, params int[] wordIndexes)
        {
            return Lifti.Querying.ScoredFieldMatch.CreateFromUnsorted(
                    score,
                    fieldId,
                    TokenLocationMatches(wordIndexes));
        }

        internal static List<ITokenLocation> TokenLocationMatches(params int[] wordIndexes)
        {
            return wordIndexes.Select(TokenMatch).ToList();
        }

        internal static List<TokenLocation> TokenLocations(params int[] wordIndexes)
        {
            return wordIndexes.Select(x => new TokenLocation(x, x, (ushort)x)).ToList();
        }

        internal static ScoredFieldMatch ScoredFieldMatch(double score, byte fieldId, params (int, int)[] compositeMatches)
        {
            return ScoredFieldMatch(
                score,
                fieldId,
                compositeMatches.Select(i => (ITokenLocation)CompositeMatch(i.Item1, i.Item2)).ToArray());
        }

        internal static ScoredFieldMatch ScoredFieldMatch(double score, byte fieldId, params ITokenLocation[] compositeMatches)
        {
            return Lifti.Querying.ScoredFieldMatch.CreateFromUnsorted(
                score,
                fieldId, 
                compositeMatches.ToList());
        }

        internal static ITokenLocation TokenMatch(int index)
        {
            return new TokenLocation(index, index, (ushort)index);
        }

        internal static ITokenLocation SingleTokenLocationMatch(int index, int start, int length)
        {
            return new TokenLocation(index, start, (ushort)length);
        }

        internal static IntermediateQueryResult IntermediateQueryResult(params ScoredToken[] matches)
        {
            return new IntermediateQueryResult(matches);
        }

        internal static ScoredToken ScoredToken(int itemId, params ScoredFieldMatch[] matches)
        {
            return new ScoredToken(
                itemId,
                matches);
        }
    }
}
