using System;
using System.IO;

namespace Lifti.Serialization.Binary
{
    /// <inheritdoc />
    public abstract class KeySerializerBase<TKey> : IKeySerializer<TKey>
    {
        private readonly Action<BinaryWriter, TKey> dataWriter;
        private readonly Func<BinaryReader, TKey> dataReader;
        private readonly Func<BinaryReader, TKey> preV5DataReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeySerializerBase{TKey}"/> class.
        /// </summary>
        /// <param name="dataWriter">The data writer.</param>
        /// <param name="dataReader">The data reader.</param>
        protected KeySerializerBase(
            Action<BinaryWriter, TKey> dataWriter,
            Func<BinaryReader, TKey> dataReader)
            : this(dataWriter, dataReader, dataReader)
        {
        }

        internal KeySerializerBase(
            Action<BinaryWriter, TKey> dataWriter,
            Func<BinaryReader, TKey> dataReader,
            Func<BinaryReader, TKey> preV5DataReader)
        {
            this.dataWriter = dataWriter;
            this.dataReader = dataReader;
            this.preV5DataReader = preV5DataReader;
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

        /// <summary>
        /// Used when reading from a pre-v5 version of the serialized index. E.g. allows
        /// integers to be read from an old index as 4 bytes, but read and write going forwards
        /// as a var int.
        /// </summary>
        internal TKey ReadV2BackwardsCompatible(BinaryReader reader)
        {
            return this.preV5DataReader(reader);
        }
    }
}
