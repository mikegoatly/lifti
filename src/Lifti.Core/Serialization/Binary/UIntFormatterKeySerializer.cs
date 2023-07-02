﻿namespace Lifti.Serialization.Binary
{
    /// <summary>
    /// An <see cref="IKeySerializer{TKey}"/> capable of handling keys of type <see cref="uint"/>.
    /// </summary>
    public class UIntFormatterKeySerializer : KeySerializerBase<uint>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UIntFormatterKeySerializer"/> class.
        /// </summary>
        public UIntFormatterKeySerializer()
            : base((w, k) => w.WriteVarUInt32(k), r => r.ReadVarUInt32(), r => r.ReadUInt32())
        {

        }
    }
}
