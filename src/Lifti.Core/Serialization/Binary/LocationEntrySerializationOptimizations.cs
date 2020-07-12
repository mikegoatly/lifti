using System;

namespace Lifti.Serialization.Binary
{
    /// <summary>
    /// Used to indicate optimisations in serialized data structures.
    /// </summary>
    [Flags]
    internal enum LocationEntrySerializationOptimizations
    {
        /// <summary>
        /// No optimizations written for this entry.
        /// </summary>
        Full = 0,

        /// <summary>
        /// The token index is written as a byte.
        /// </summary>
        TokenIndexByte = 1 << 0,

        /// <summary>
        /// The token index is written as a uint16.
        /// </summary>
        TokenIndexUInt16 = 1 << 1,

        /// <summary>
        /// The token start is written as a byte.
        /// </summary>
        TokenStartByte = 1 << 2,

        /// <summary>
        /// The token start is written as a uint16.
        /// </summary>
        TokenStartUInt16 = 1 << 3,

        /// <summary>
        /// The length of the token is not serialized as it is the same as the length of the previously serialized token and can be inferred.
        /// </summary>
        LengthSameAsLast = 1 << 4
    }
}
