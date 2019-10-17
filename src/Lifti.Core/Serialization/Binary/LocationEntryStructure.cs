using System;

namespace Lifti.Serialization.Binary
{
    [Flags]
    internal enum LocationEntryStructure
    {
        Full = 0,
        WordIndexByte = 1 << 0,
        WordIndexUInt16 = 1 << 1,
        WordStartByte = 1 << 2,
        WordStartUInt16 = 1 << 3,
        LengthSameAsLast = 1 << 4
    }
}
