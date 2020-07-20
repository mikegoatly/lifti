namespace Lifti.Serialization.Binary
{
    /// <summary>
    /// An <see cref="IKeySerializer{TKey}"/> capable of handling keys of type <see cref="string"/>.
    /// </summary>
    public class StringFormatterKeySerializer : KeySerializerBase<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringFormatterKeySerializer"/> class.
        /// </summary>
        public StringFormatterKeySerializer()
            : base((w, k) => w.Write(k), r => r.ReadString())
        {

        }
    }
}
