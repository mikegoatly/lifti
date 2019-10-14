namespace Lifti.Serialization.Binary
{
    public class UIntFormatterKeySerializer : KeySerializerBase<uint>
    {
        public UIntFormatterKeySerializer()
            : base((w, k) => w.Write(k), r => r.ReadUInt32())
        {

        }
    }
}
