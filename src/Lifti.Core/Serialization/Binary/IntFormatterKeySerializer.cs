namespace Lifti.Serialization.Binary
{
    public class IntFormatterKeySerializer : KeySerializerBase<int>
    {
        public IntFormatterKeySerializer()
            : base((w, k) => w.Write(k), r => r.ReadInt32())
        {

        }
    }
}
