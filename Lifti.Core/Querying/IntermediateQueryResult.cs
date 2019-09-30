using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Should not be compared")]
    public struct IntermediateQueryResult
    {
        public static IntermediateQueryResult Empty { get; } = new IntermediateQueryResult(Array.Empty<QueryWordMatch>());

        public IntermediateQueryResult(IEnumerable<QueryWordMatch> matches)
        {
            this.Matches = matches;
        }

        public IEnumerable<QueryWordMatch> Matches { get; }

        /// <summary>
        /// Intersects this and the specified instance, but only when the positions of the matched words are within a given tolerance.
        /// </summary>
        public IntermediateQueryResult PositionalIntersect(IntermediateQueryResult results, int leftTolerance, int rightTolerance)
        {
            return new IntermediateQueryResult(PositionalIntersectEnumerator(this, results, leftTolerance, rightTolerance));
        }

        /// <summary>
        /// Intersects this and the specified instance, but only when the positions of the matched words are within a given tolerance.
        /// </summary>
        public IntermediateQueryResult PositionalIntersectAndCombine(IntermediateQueryResult results, int leftTolerance, int rightTolerance)
        {
            return new IntermediateQueryResult(PositionalIntersectAndCombineEnumerator(this, results, leftTolerance, rightTolerance));
        }

        /// <summary>
        /// Intersects this and the specified instance - this is the equivalent of an AND statement.
        /// </summary>
        public IntermediateQueryResult Intersect(IntermediateQueryResult results)
        {
            return new IntermediateQueryResult(IntersectEnumerator(this, results));
        }

        /// <summary>
        /// Union this and the specified instance - this is the equivalent of an OR statement.
        /// </summary>
        public IntermediateQueryResult Union(IntermediateQueryResult results)
        {
            return new IntermediateQueryResult(UnionEnumerator(this, results));
        }

        private static IEnumerable<QueryWordMatch> PositionalIntersectEnumerator(IntermediateQueryResult current, IntermediateQueryResult next, int leftTolerance, int rightTolerance)
        {
            var (currentLookup, nextLookup) = BuildLookups(current, next);

            foreach (var match in currentLookup)
            {
                if (nextLookup.Contains(match.Key))
                {
                    var positionalMatches = PositionallyMatchWords(
                        match.SelectMany(m => m.FieldMatches),
                        nextLookup[match.Key].SelectMany(m => m.FieldMatches),
                        leftTolerance,
                        rightTolerance);

                    if (positionalMatches.Count > 0)
                    {
                        yield return new QueryWordMatch(match.Key, positionalMatches);
                    }
                }
            }
        }

        private static IReadOnlyList<FieldMatch> PositionallyMatchWords(IEnumerable<FieldMatch> currentWords, IEnumerable<FieldMatch> nextWords, int leftTolerance, int rightTolerance)
        {
            var matchedFields = currentWords.Join(
                nextWords,
                o => o.FieldId,
                o => o.FieldId,
                (inner, outer) => (inner.FieldId, currentLocations: inner.Locations, nextLocations: outer.Locations))
                .ToList();

            var fieldResults = new List<FieldMatch>(matchedFields.Count);
            var fieldWordMatches = new HashSet<IWordLocationMatch>();
            foreach (var (fieldId, currentLocations, nextLocations) in matchedFields)
            {
                fieldWordMatches.Clear();

                // TODO Unoptimised O(n^2) implementation for now - big optimisations be made when location order can be guaranteed
                foreach (var currentWord in currentLocations)
                {
                    foreach (var nextWord in nextLocations)
                    {
                        if (leftTolerance > 0)
                        {
                            if ((nextWord.MaxWordIndex - currentWord.MinWordIndex).IsPositiveAndLessThanOrEqualTo(leftTolerance))
                            {
                                fieldWordMatches.Add(currentWord);
                                fieldWordMatches.Add(nextWord);
                            }
                        }

                        if (rightTolerance > 0)
                        {
                            if ((currentWord.MaxWordIndex - nextWord.MinWordIndex).IsPositiveAndLessThanOrEqualTo(rightTolerance))
                            {
                                fieldWordMatches.Add(currentWord);
                                fieldWordMatches.Add(nextWord);
                            }
                        }
                    }
                }

                if (fieldWordMatches.Count > 0)
                {
                    fieldResults.Add(new FieldMatch(fieldId, fieldWordMatches.ToList()));
                }
            }

            return fieldResults;
        }

        private static IEnumerable<QueryWordMatch> PositionalIntersectAndCombineEnumerator(IntermediateQueryResult current, IntermediateQueryResult next, int leftTolerance, int rightTolerance)
        {
            var (currentLookup, nextLookup) = BuildLookups(current, next);

            foreach (var match in currentLookup)
            {
                if (nextLookup.Contains(match.Key))
                {
                    var positionalMatches = PositionallyMatchAndCombineWords(
                        match.SelectMany(m => m.FieldMatches),
                        nextLookup[match.Key].SelectMany(m => m.FieldMatches),
                        leftTolerance,
                        rightTolerance);

                    if (positionalMatches.Count > 0)
                    {
                        yield return new QueryWordMatch(match.Key, positionalMatches);
                    }
                }
            }
        }

        private static IReadOnlyList<FieldMatch> PositionallyMatchAndCombineWords(IEnumerable<FieldMatch> currentWords, IEnumerable<FieldMatch> nextWords, int leftTolerance, int rightTolerance)
        {
            var matchedFields = currentWords.Join(
                nextWords,
                o => o.FieldId,
                o => o.FieldId,
                (inner, outer) => (inner.FieldId, currentLocations: inner.Locations, nextLocations: outer.Locations))
                .ToList();

            var fieldResults = new List<FieldMatch>(matchedFields.Count);
            var fieldWordMatches = new List<IWordLocationMatch>();
            foreach (var (fieldId, currentLocations, nextLocations) in matchedFields)
            {
                fieldWordMatches.Clear();

                // TODO Unoptimised O(n^2) implementation for now - big optimisations be made when location order can be guaranteed
                foreach (var currentWord in currentLocations)
                {
                    foreach (var nextWord in nextLocations)
                    {
                        if (leftTolerance > 0)
                        {
                            if ((nextWord.MaxWordIndex - currentWord.MinWordIndex).IsPositiveAndLessThanOrEqualTo(leftTolerance))
                            {
                                fieldWordMatches.Add(new CompositeWordMatchLocation(currentWord, nextWord));
                            }
                        }

                        if (rightTolerance > 0)
                        {
                            if ((currentWord.MaxWordIndex - nextWord.MinWordIndex).IsPositiveAndLessThanOrEqualTo(rightTolerance))
                            {
                                fieldWordMatches.Add(new CompositeWordMatchLocation(currentWord, nextWord));
                            }
                        }
                    }
                }

                if (fieldWordMatches.Count > 0)
                {
                    fieldResults.Add(new FieldMatch(fieldId, fieldWordMatches.ToList()));
                }
            }

            return fieldResults;
        }

        private static IEnumerable<QueryWordMatch> IntersectEnumerator(IntermediateQueryResult current, IntermediateQueryResult next)
        {
            var (currentLookup, nextLookup) = BuildLookups(current, next);

            foreach (var match in currentLookup)
            {
                if (nextLookup.Contains(match.Key))
                {
                    yield return new QueryWordMatch(
                        match.Key,
                        match.SelectMany(m => m.FieldMatches)
                            .Concat(nextLookup[match.Key].SelectMany(m => m.FieldMatches)));
                }
            }
        }

        private static IEnumerable<QueryWordMatch> UnionEnumerator(IntermediateQueryResult current, IntermediateQueryResult next)
        {
            var (currentLookup, nextLookup) = BuildLookups(current, next);
            var nextDictionary = nextLookup.ToDictionary(i => i.Key, i => i);

            foreach (var match in currentLookup)
            {
                if (nextDictionary.TryGetValue(match.Key, out var nextLocations))
                {
                    // Exists in both
                    yield return new QueryWordMatch(
                        match.Key,
                        match.SelectMany(m => m.FieldMatches)
                            .Concat(nextLocations.SelectMany(m => m.FieldMatches)));

                    nextDictionary.Remove(match.Key);
                }
                else
                {
                    // Exists only in current
                    yield return new QueryWordMatch(match.Key, match.SelectMany(m => m.FieldMatches));
                }
            }

            // Any items still remaining in nextDictionary exist only in the new results so can just be yielded
            foreach (var match in nextDictionary)
            {
                yield return new QueryWordMatch(match.Key, match.Value.SelectMany(m => m.FieldMatches));
            }
        }

        private static (ILookup<int, QueryWordMatch> currentLookup, ILookup<int, QueryWordMatch> nextLookup) BuildLookups(IntermediateQueryResult current, IntermediateQueryResult next)
        {
            return
                (
                    currentLookup: current.Matches.ToLookup(m => m.ItemId),
                    nextLookup: next.Matches.ToLookup(m => m.ItemId)
                );
        }
    }
}
