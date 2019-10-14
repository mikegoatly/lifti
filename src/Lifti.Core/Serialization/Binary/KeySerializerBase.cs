using System;
using System.IO;

namespace Lifti.Serialization.Binary
{
    public abstract class KeySerializerBase<TKey> : IKeySerializer<TKey>
    {
        private readonly Action<BinaryWriter, TKey> dataWriter;
        private readonly Func<BinaryReader, TKey> dataReader;

        public KeySerializerBase(Action<BinaryWriter, TKey> dataWriter, Func<BinaryReader, TKey> dataReader)
        {
            this.dataWriter = dataWriter;
            this.dataReader = dataReader;
        }

        public TKey Read(BinaryReader reader)
        {
            return this.dataReader(reader);
        }

        public void Write(BinaryWriter writer, TKey key)
        {
            this.dataWriter(writer, key);
        }
    }
}
