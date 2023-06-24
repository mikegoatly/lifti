namespace Lifti.Serialization.Binary
{
    /// <summary>
    /// An implementation of <see cref="IKeySerializer{TKey}"/> capable of reading
    /// <see cref="int"/> keys.
    /// </summary>
    public class IntFormatterKeySerializer : KeySerializerBase<int>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntFormatterKeySerializer"/> class.
        /// </summary>
        public IntFormatterKeySerializer()
            : base((w, k) => w.WriteVarInt32(k), r => r.ReadVarInt32())
        {

        }
    }
}
