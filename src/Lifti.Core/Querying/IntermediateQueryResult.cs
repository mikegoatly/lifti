﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Lifti.Querying
{
    /// <summary>
    /// A partial search result that can subsequently be combined with other <see cref="IntermediateQueryResult"/> instances
    /// materialized as part of a query.
    /// </summary>
    public readonly struct IntermediateQueryResult : IEquatable<IntermediateQueryResult>
    {
        internal IntermediateQueryResult(List<ScoredToken> matches, bool assumeSorted)
        {
            if (!assumeSorted)
            {
                matches.Sort((x, y) => x.DocumentId.CompareTo(y.DocumentId));
            }

            this.Matches = matches;

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
        public static IntermediateQueryResult Empty { get; } = new IntermediateQueryResult([], true);

        /// <summary>
        /// Gets the set of <see cref="ScoredToken"/> matches that this instance captured.
        /// </summary>
        public IReadOnlyList<ScoredToken> Matches { get; }

        /// <summary>
        /// Intersects this and the specified instance, but only when the positions of the matched tokens on the left are preceding the tokens on the right.
        /// </summary>
        public IntermediateQueryResult PrecedingIntersect(IntermediateQueryResult results)
        {
            // If either of the two results sets involved are empty, then there is no intersection, so 
            // we can just return an empty result set
            if (this.Matches.Count == 0 || results.Matches.Count == 0)
            {
                return Empty;
            }

            return new IntermediateQueryResult(
                PrecedingIntersectMerger.Apply(this, results),
                true);
        }

        /// <summary>
        /// Intersects this and the specified instance, but only when the positions of the matched tokens are within a given tolerance. Matching tokens are combined
        /// into <see cref="CompositeTokenLocation"/> instances.
        /// </summary>
        public IntermediateQueryResult CompositePositionalIntersect(IntermediateQueryResult results, int leftTolerance, int rightTolerance)
        {
            // If either of the two results sets involved are empty, then there is no intersection, so 
            // we can just return an empty result set
            if (this.Matches.Count == 0 || results.Matches.Count == 0)
            {
                return Empty;
            }

            return new IntermediateQueryResult(
                CompositePositionalIntersectMerger.Apply(this, results, leftTolerance, rightTolerance),
                true);
        }

        /// <summary>
        /// Intersects this and the specified instance - this is the equivalent of an AND statement.
        /// </summary>
        public IntermediateQueryResult Intersect(IntermediateQueryResult results)
        {
            // If either of the two results sets involved are empty, then there is no intersection, so 
            // we can just return an empty result set
            if (this.Matches.Count == 0 || results.Matches.Count == 0)
            {
                return Empty;
            }

            return new IntermediateQueryResult(
                IntersectMerger.Apply(this, results),
                true);
        }

        /// <summary>
        /// Union this and the specified instance - this is the equivalent of an OR statement.
        /// </summary>
        public IntermediateQueryResult Union(IntermediateQueryResult results)
        {
            // We can shortcut the unioning logic if either of the two results sets involved are empty
            // In this case we can just return the other result set
            if (this.Matches.Count == 0)
            {
                return results;
            }

            if (results.Matches.Count == 0)
            {
                return this;
            }

            return new IntermediateQueryResult(
                UnionMerger.Apply(this, results),
                true);
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

        internal HashSet<int> ToDocumentIdLookup()
        {
            var lookup = new HashSet<int>();
#if !NETSTANDARD
            lookup.EnsureCapacity(this.Matches.Count);
#endif
            for (var i = 0; i < this.Matches.Count; i++)
            {
                lookup.Add(this.Matches[i].DocumentId);
            }

            return lookup;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Matches);
        }

        /// <inheritdoc />
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is IntermediateQueryResult result &&
                   this.Equals(result);
        }

        /// <inheritdoc />
        public bool Equals(IntermediateQueryResult other)
        {
            return this.Matches.SequenceEqual(other.Matches);
        }

        /// <inheritdoc />
        public static bool operator ==(IntermediateQueryResult left, IntermediateQueryResult right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc />
        public static bool operator !=(IntermediateQueryResult left, IntermediateQueryResult right)
        {
            return !(left == right);
        }
    }
}
