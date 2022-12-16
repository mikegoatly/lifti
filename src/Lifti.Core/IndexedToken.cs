using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    /// <summary>
    /// Information about a token from a document that has been stored an index.
    /// </summary>
    public readonly struct IndexedToken : IEquatable<IndexedToken>
    {
        /// <summary>
        /// Constructs a new <see cref="IndexedToken"/> instance.
        /// </summary>
        public IndexedToken(byte fieldId, params TokenLocation[] locations)
        {
            this.FieldId = fieldId;
            this.Locations = locations;
        }

        /// <summary>
        /// Constructs a new <see cref="IndexedToken"/> instance.
        /// </summary>
        public IndexedToken(byte fieldId, IReadOnlyList<TokenLocation> locations)
        {
            this.FieldId = fieldId;
            this.Locations = locations;
        }

        /// <summary>
        /// Gets the id of the field.
        /// </summary>
        public byte FieldId { get; }

        /// <summary>
        /// Gets the set of <see cref="TokenLocation"/> instances for all the locations
        /// that this token was found in the document.
        /// </summary>
        public IReadOnlyList<TokenLocation> Locations { get; }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is IndexedToken location &&
                this.Equals(location);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = HashCode.Combine(this.FieldId);
            foreach (var location in this.Locations)
            {
                hashCode = HashCode.Combine(hashCode, location);
            }

            return hashCode;
        }

        /// <inheritdoc />
        public bool Equals(IndexedToken other)
        {
            return this.FieldId == other.FieldId &&
                   this.Locations.SequenceEqual(other.Locations);
        }

        /// <inheritdoc />
        public static bool operator ==(IndexedToken left, IndexedToken right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc />
        public static bool operator !=(IndexedToken left, IndexedToken right)
        {
            return !(left == right);
        }
    }
}
