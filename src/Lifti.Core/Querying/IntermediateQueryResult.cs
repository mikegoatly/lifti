using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lifti.Querying
{
    /// <summary>
    /// A partial search result that can subsequently be combined with other <see cref="IntermediateQueryResult"/> instances
    /// materialized as part of a query.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Should not be compared")]
    public readonly struct IntermediateQueryResult
    {
        /// <summary>
        /// Creates a new instance of <see cref="IntermediateQueryResult"/>.
        /// </summary>
        public IntermediateQueryResult(IEnumerable<ScoredToken> matches)
        {
            this.Matches = matches as IReadOnlyList<ScoredToken> ?? matches.ToList();

#if DEBUG
            // Verify that we are in document id order, and that there are no duplicates
            for (var i = 0; i < this.Matches.Count; i++)
            {
                if (i > 0)
                {
                    var previous = this.Matches[i - 1].DocumentId;
                    var next = this.Matches[i].DocumentId;
                    if (previous > next)
                    {
                        System.Diagnostics.Debug.Fail("Intermediate query results must be in document id order");
                    }
                    else if (previous == next)
                    {
                        System.Diagnostics.Debug.Fail("Duplicate document id encountered in intermediate query results");
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Gets an <see cref="IntermediateQueryResult"/> with no matches.
        /// </summary>
        public static IntermediateQueryResult Empty { get; } = new IntermediateQueryResult(Array.Empty<ScoredToken>());

        /// <summary>
        /// Gets the set of <see cref="ScoredToken"/> matches that this instance captured.
        /// </summary>
        public IReadOnlyList<ScoredToken> Matches { get; }

        /// <summary>
        /// Intersects this and the specified instance, but only when the positions of the matched tokens on the left are preceding the tokens on the right.
        /// </summary>
        public IntermediateQueryResult PrecedingIntersect(IntermediateQueryResult results)
        {
            return new IntermediateQueryResult(PrecedingIntersectMerger.Apply(this, results));
        }

        /// <summary>
        /// Intersects this and the specified instance, but only when the positions of the matched tokens are within a given tolerance. Matching tokens are combined
        /// into <see cref="CompositeTokenMatchLocation"/> instances.
        /// </summary>
        public IntermediateQueryResult CompositePositionalIntersect(IntermediateQueryResult results, int leftTolerance, int rightTolerance)
        {
            return new IntermediateQueryResult(CompositePositionalIntersectMerger.Apply(this, results, leftTolerance, rightTolerance));
        }

        /// <summary>
        /// Intersects this and the specified instance - this is the equivalent of an AND statement.
        /// </summary>
        public IntermediateQueryResult Intersect(IntermediateQueryResult results)
        {
            return new IntermediateQueryResult(IntersectMerger.Apply(this, results));
        }

        /// <summary>
        /// Union this and the specified instance - this is the equivalent of an OR statement.
        /// </summary>
        public IntermediateQueryResult Union(IntermediateQueryResult results)
        {
            return new IntermediateQueryResult(UnionMerger.Apply(this, results));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var match in this.Matches)
            {
                match.ToString(builder);
            }

            return builder.ToString();
        }
    }
}
