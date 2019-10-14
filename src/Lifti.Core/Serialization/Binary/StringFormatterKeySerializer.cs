namespace Lifti.Serialization.Binary
{
    public class StringFormatterKeySerializer : KeySerializerBase<string>
    {
        public StringFormatterKeySerializer()
            : base((w, k) => w.Write(k), r => r.ReadString())
        {

        }
    }
}
