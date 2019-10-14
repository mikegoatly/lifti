using System;

namespace Lifti.Serialization.Binary
{
    public class GuidFormatterKeySerializer : KeySerializerBase<Guid>
    {
        public GuidFormatterKeySerializer()
            : base((w, k) => w.Write(k.ToByteArray()), r => new Guid(r.ReadBytes(16)))
        {

        }
    }
}
