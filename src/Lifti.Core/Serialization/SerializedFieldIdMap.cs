using System.Collections.Generic;

namespace Lifti.Serialization
{
    /// <summary>
    /// Provides a map between the field ids in a serialized index and the field ids in the index as it is now structured.
    /// </summary>
    public readonly record struct SerializedFieldIdMap
    {
        private readonly Dictionary<byte, byte> fieldIdMap;

        internal SerializedFieldIdMap(Dictionary<byte, byte> fieldIdMap)
        {
            this.fieldIdMap = fieldIdMap;
        }

        /// <summary>
        /// Maps a field id from the serialized index to the field id in the index as it is now structured.
        /// </summary>
        public byte Map(byte serializedFieldId)
        {
            return this.fieldIdMap[serializedFieldId];
        }
    }
}
