using System.IO;

namespace Lifti.Serialization.Binary
{
    internal class V3IndexReader<TKey> : V2IndexReader<TKey>
        where TKey : notnull
    {
        public V3IndexReader(Stream stream, bool disposeStream, IKeySerializer<TKey> keySerializer) 
            : base(stream, disposeStream, keySerializer)
        {
        }

        /// <summary>
        /// Reads a character matched at a node.
        /// </summary>
        protected override char ReadMatchedCharacter()
        {
            return (char)this.reader.ReadInt16();
        }
    }
}
