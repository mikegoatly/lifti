using Lifti.Querying;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tests.Querying
{
    public abstract class QueryTestBase
    {
        internal static CompositeTokenLocation CompositeTokenLocation(int leftWordIndex, int rightWordIndex)
        {
            return new CompositeTokenLocation(
                [.. TokenLocations(leftWordIndex, rightWordIndex)],
                Math.Min(leftWordIndex, rightWordIndex),
                Math.Max(leftWordIndex, rightWordIndex));
        }

        internal static CompositeTokenLocation CompositeTokenLocation(params int[] wordIndexes)
        {
            return new CompositeTokenLocation(
                 [.. TokenLocations(wordIndexes)],
                 wordIndexes.Min(),
                 wordIndexes.Max());
        }

        internal static ScoredFieldMatch ScoredFieldMatch(double score, byte fieldId, params int[] wordIndexes)
        {
            return Lifti.Querying.ScoredFieldMatch.CreateFromPresorted(
                    score,
                    fieldId,
                    TokenLocations(wordIndexes));
        }

        internal static List<TokenLocation> TokenLocations(params int[] wordIndexes)
        {
            return wordIndexes.Select(TokenLocation).ToList();
        }

        internal static ScoredFieldMatch ScoredFieldMatch(double score, byte fieldId, params (int, int)[] compositeMatches)
        {
            return ScoredFieldMatch(
                score,
                fieldId,
                compositeMatches.Select(i => (ITokenLocation)CompositeTokenLocation(i.Item1, i.Item2)).ToArray());
        }

        internal static ScoredFieldMatch ScoredFieldMatch(double score, byte fieldId, params ITokenLocation[] compositeMatches)
        {
            return Lifti.Querying.ScoredFieldMatch.CreateFromUnsorted(
                score,
                fieldId,
                [.. compositeMatches]);
        }

        internal static TokenLocation TokenLocation(int index)
        {
            return new TokenLocation(index, index, (ushort)index);
        }

        internal static TokenLocation TokenLocation(int index, int start, int length)
        {
            return new TokenLocation(index, start, (ushort)length);
        }

        internal static IntermediateQueryResult IntermediateQueryResult(params ScoredToken[] matches)
        {
            return new IntermediateQueryResult(matches);
        }

        internal static ScoredToken ScoredToken(int documentId, params ScoredFieldMatch[] matches)
        {
            return new ScoredToken(
                documentId,
                matches);
        }
    }
}
