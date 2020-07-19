using System;

namespace Lifti.Serialization.Binary
{
    /// <summary>
    /// A <see cref="IKeySerializer{TKey}"/> capable of handling <see cref="System.Guid"/>s.
    /// </summary>
    public class GuidFormatterKeySerializer : KeySerializerBase<Guid>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GuidFormatterKeySerializer" /> class.
        /// </summary>
        public GuidFormatterKeySerializer()
            : base((w, k) => w.Write(k.ToByteArray()), r => new Guid(r.ReadBytes(16)))
        {
        }
    }
}
