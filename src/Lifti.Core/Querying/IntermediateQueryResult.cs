using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Should not be compared")]
    public struct IntermediateQueryResult
    {
        public IntermediateQueryResult(IEnumerable<ScoredToken> matches)
        {
            this.Matches = matches as IReadOnlyList<ScoredToken> ?? matches.ToList();
        }

        public static IntermediateQueryResult Empty { get; } = new IntermediateQueryResult(Array.Empty<ScoredToken>());

        public IReadOnlyList<ScoredToken> Matches { get; }

        /// <summary>
        /// Intersects this and the specified instance, but only when the positions of the matched tokens on the left are preceding the tokens on the right.
        /// </summary>
        public IntermediateQueryResult PrecedingIntersect(IntermediateQueryResult results)
        {
            return new IntermediateQueryResult(PrecedingIntersectMerger.Instance.Apply(this, results));
        }

        /// <summary>
        /// Intersects this and the specified instance, but only when the positions of the matched tokens are within a given tolerance. Matching tokens are combined
        /// into <see cref="CompositeTokenMatchLocation"/> instances.
        /// </summary>
        public IntermediateQueryResult CompositePositionalIntersect(IntermediateQueryResult results, int leftTolerance, int rightTolerance)
        {
            return new IntermediateQueryResult(CompositePositionalIntersectMerger.Instance.Apply(this, results, leftTolerance, rightTolerance));
        }

        /// <summary>
        /// Intersects this and the specified instance - this is the equivalent of an AND statement.
        /// </summary>
        public IntermediateQueryResult Intersect(IntermediateQueryResult results)
        {
            return new IntermediateQueryResult(IntersectMerger.Instance.Apply(this, results));
        }

        /// <summary>
        /// Union this and the specified instance - this is the equivalent of an OR statement.
        /// </summary>
        public IntermediateQueryResult Union(IntermediateQueryResult results)
        {
            return new IntermediateQueryResult(UnionMerger.Instance.Apply(this, results));
        }
    }
}
