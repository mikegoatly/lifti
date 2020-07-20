using System;
using System.IO;

namespace Lifti.Serialization.Binary
{
    /// <inheritdoc />
    public abstract class KeySerializerBase<TKey> : IKeySerializer<TKey>
    {
        private readonly Action<BinaryWriter, TKey> dataWriter;
        private readonly Func<BinaryReader, TKey> dataReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeySerializerBase{TKey}"/> class.
        /// </summary>
        /// <param name="dataWriter">The data writer.</param>
        /// <param name="dataReader">The data reader.</param>
        protected KeySerializerBase(Action<BinaryWriter, TKey> dataWriter, Func<BinaryReader, TKey> dataReader)
        {
            this.dataWriter = dataWriter;
            this.dataReader = dataReader;
        }

        /// <inheritdoc />
        public TKey Read(BinaryReader reader)
        {
            return this.dataReader(reader);
        }

        /// <inheritdoc />
        public void Write(BinaryWriter writer, TKey key)
        {
            this.dataWriter(writer, key);
        }
    }
}
