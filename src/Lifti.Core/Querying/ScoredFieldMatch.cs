using System;
using System.Collections.Generic;

namespace Lifti.Querying
{
    public struct ScoredFieldMatch : IEquatable<ScoredFieldMatch>
    {
        public ScoredFieldMatch(double score, FieldMatch fieldMatch)
        {
            this.Score = score;
            this.FieldMatch = fieldMatch;
        }

        public double Score { get; }
        public byte FieldId => this.FieldMatch.FieldId;
        public IReadOnlyList<ITokenLocationMatch> Locations => this.FieldMatch.Locations;

        public FieldMatch FieldMatch { get; }

        public override bool Equals(object? obj)
        {
            return obj is ScoredFieldMatch match &&
                   this.Equals(match);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Score, this.FieldMatch);
        }

        public bool Equals(ScoredFieldMatch other)
        {
            return this.Score == other.Score &&
                   this.FieldMatch.Equals(other.FieldMatch);
        }

        public static bool operator ==(ScoredFieldMatch left, ScoredFieldMatch right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ScoredFieldMatch left, ScoredFieldMatch right)
        {
            return !(left == right);
        }
    }
}
