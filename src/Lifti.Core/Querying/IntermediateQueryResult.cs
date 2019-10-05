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
            this.Matches = matches.ToList();
        }

        public IReadOnlyList<QueryWordMatch> Matches { get; }

        /// <summary>
        /// Intersects this and the specified instance, but only when the positions of the matched words on the left are preceding the words on the right.
        /// </summary>
        public IntermediateQueryResult PrecedingIntersect(IntermediateQueryResult results)
        {
            return new IntermediateQueryResult(PrecedingIntersectMerger.Instance.Apply(this, results));
        }

        /// <summary>
        /// Intersects this and the specified instance, but only when the positions of the matched words are within a given tolerance. Matching words are combined
        /// into <see cref="CompositeWordMatchLocation"/> instances.
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
