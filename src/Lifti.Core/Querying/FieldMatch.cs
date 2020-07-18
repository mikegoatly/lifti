using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// Describes a match that occurred for a query within a field.
    /// </summary>
    public struct FieldMatch : IEquatable<FieldMatch>
    {
        /// <summary>
        /// Constructs a new instance of <see cref="FieldMatch"/>.
        /// </summary>
        public FieldMatch(IndexedToken token)
            : this(token.FieldId, token.Locations)
        {
        }

        /// <summary>
        /// Constructs a new instance of <see cref="FieldMatch"/>.
        /// </summary>
        public FieldMatch(byte fieldId, IReadOnlyList<ITokenLocationMatch> locations)
        {
            this.FieldId = fieldId;
            this.Locations = locations;
        }

        /// <summary>
        /// Constructs a new instance of <see cref="FieldMatch"/>.
        /// </summary>
        public FieldMatch(byte fieldId, params ITokenLocationMatch[] locations)
        {
            this.FieldId = fieldId;
            this.Locations = locations;
        }

        private FieldMatch(byte fieldId, IReadOnlyList<TokenLocation> locations)
        {
            this.FieldId = fieldId;
            this.Locations = locations.Select(l => (ITokenLocationMatch)new SingleTokenLocationMatch(l)).ToList();
        }

        /// <summary>
        /// Gets the id of the matched field.
        /// </summary>
        public byte FieldId { get; }

        /// <summary>
        /// Gets the set of <see cref="ITokenLocationMatch"/> that describe where in the document the matches occurred.
        /// </summary>
        public IReadOnlyList<ITokenLocationMatch> Locations { get; }

        /// <summary>
        /// Enumerates through all the <see cref="Locations"/> and expands them to a set of <see cref="TokenLocation"/>s.
        /// </summary>
        public IReadOnlyList<TokenLocation> GetTokenLocations()
        {
            return this.Locations.SelectMany(l => l.GetLocations())
                .Distinct()
                .OrderBy(l => l.TokenIndex)
                .ToList();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is FieldMatch match &&
                this.Equals(match);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.FieldId, this.Locations);
        }

        /// <inheritdoc />
        public bool Equals(FieldMatch other)
        {
            return this.FieldId == other.FieldId &&
                   this.Locations.SequenceEqual(other.Locations);
        }

        /// <inheritdoc />
        public static bool operator ==(FieldMatch left, FieldMatch right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc />
        public static bool operator !=(FieldMatch left, FieldMatch right)
        {
            return !(left == right);
        }
    }
}
