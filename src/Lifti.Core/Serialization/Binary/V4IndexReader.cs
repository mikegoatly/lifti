using System.IO;

namespace Lifti.Serialization.Binary
{
    internal class V4IndexReader<TKey> : V3IndexReader<TKey>
        where TKey : notnull
    {
        public V4IndexReader(Stream stream, bool disposeStream, IKeySerializer<TKey> keySerializer)
            : base(stream, disposeStream, keySerializer)
        {
        }

        /// <inheritdoc />
        protected override char[] ReadIntraNodeText(int textLength)
        {
            var noSurrogateCharacters = reader.ReadBoolean();

            if (noSurrogateCharacters)
            {
                // Read as characters as per previous version
                return base.ReadIntraNodeText(textLength);
            }

            // Read characters serialized as Int16s
            var data = new char[textLength];
            for (var i = 0; i < textLength; i++)
            {
                data[i] = (char)this.reader.ReadInt16();
            }

            return data;
        }
    }
}
