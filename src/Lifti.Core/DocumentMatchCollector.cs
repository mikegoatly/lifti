using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    namespace Lifti.Querying
    {
        /// <summary>
        /// A helper class that allows matches to be collected together efficiently prior to
        /// creating a <see cref="IntermediateQueryResult"/> instance.
        /// </summary>
        public sealed class DocumentMatchCollector
        {
            private readonly Dictionary<int, FieldMatches> documentMatches = [];

            internal void Add(int documentId, byte fieldId, IReadOnlyList<TokenLocation> tokenLocations, double score)
            {
                if (!this.documentMatches.TryGetValue(documentId, out var fieldMatches))
                {
                    fieldMatches = new();
                    this.documentMatches.Add(documentId, fieldMatches);
                }

                fieldMatches.Add(fieldId, score, tokenLocations);
            }

            /// <summary>
            /// Completes the match collection process and converts this instance into an <see cref="IntermediateQueryResult"/>.
            /// Once this method has been called, this instance should no longer be used.
            /// </summary>
            public IntermediateQueryResult ToIntermediateQueryResult()
            {
                var results = new List<ScoredToken>(this.documentMatches.Count);
                
                results.AddRange(
                    this.documentMatches.Select(
                        d => new ScoredToken(d.Key, d.Value.ToScoredFieldMatches())));

                return new IntermediateQueryResult(results, false);
            }
        }

        internal class FieldMatches
        {
            private readonly Dictionary<byte, FieldMatchCollector> fieldLookup = [];

            public void Add(byte fieldId, double score, IReadOnlyList<TokenLocation> tokenLocations)
            {
                if (!this.fieldLookup.TryGetValue(fieldId, out var fieldMatchCollector))
                {
                    fieldMatchCollector = new();
                    this.fieldLookup.Add(fieldId, fieldMatchCollector);
                }

                fieldMatchCollector.Add(score, tokenLocations);
            }

            public ScoredFieldMatch[] ToScoredFieldMatches()
            {
                var results = new ScoredFieldMatch[this.fieldLookup.Count];

                var i = 0;
                foreach (var fieldMatch in this.fieldLookup)
                {
                    results[i++] = fieldMatch.Value.ToScoredToken(fieldMatch.Key);
                }

                return results;
            }
        }

        internal class FieldMatchCollector
        {
            private int additionCount;
            private readonly List<TokenLocation> fieldLocations = [];
            public double Score { get; private set; }

            public void Add(double score, IReadOnlyList<TokenLocation> tokenLocations)
            {
                this.Score += score;

#if !NETSTANDARD
                this.fieldLocations.EnsureCapacity(this.fieldLocations.Count + tokenLocations.Count);
#endif

                this.fieldLocations.AddRange(tokenLocations);
                this.additionCount++;
            }

            internal ScoredFieldMatch ToScoredToken(byte fieldId)
            {
                if (this.additionCount > 1)
                {
                    // Ensure the locations are sorted
                    this.fieldLocations.Sort();
                }

                return ScoredFieldMatch.CreateFromPresorted(this.Score, fieldId, this.fieldLocations);
            }
        }
    }
}
