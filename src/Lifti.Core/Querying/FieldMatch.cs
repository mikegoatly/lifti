using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// Describes a match that occurred for a query within a field.
    /// </summary>
    public readonly struct FieldMatch : IEquatable<FieldMatch>
    {
        /// <summary>
        /// Constructs a new instance of <see cref="FieldMatch"/> from a <see cref="IndexedToken"/>.
        /// </summary>
        public FieldMatch(IndexedToken token)
        {
            this.FieldId = token.FieldId;
            this.Locations = token.Locations.Select(l => (ITokenLocationMatch)new SingleTokenLocationMatch(l)).ToList();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="FieldMatch"/>.
        /// </summary>
        public FieldMatch(byte fieldId, IEnumerable<ITokenLocationMatch> locations)
        {
            this.FieldId = fieldId;
            this.Locations = CreateLocationsList(locations);
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
        public override bool Equals(object? obj)
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

        private static List<ITokenLocationMatch> CreateLocationsList(IEnumerable<ITokenLocationMatch> matches)
        {
            return matches.OrderBy(x => x.MinTokenIndex).ToList();
        }
    }
}
