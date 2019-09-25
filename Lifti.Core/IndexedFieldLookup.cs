using System.Collections.Generic;
using System.Threading;

namespace Lifti
{
    public class IndexedFieldLookup : IIndexedFieldLookup
    {
        private readonly Dictionary<string, byte> fieldToIdLookup = new Dictionary<string, byte>();
        private readonly Dictionary<byte, string> idToFieldLookup = new Dictionary<byte, string>();
        private int nextId = 0;

        public byte DefaultField { get; } = 0;

        public string GetFieldForId(byte id)
        {
            if (id == 0)
            {
                return "Unspecified";
            }
            else if (idToFieldLookup.TryGetValue(id, out var fieldName))
            {
                return fieldName;
            }

            throw new LiftiException(ExceptionMessages.FieldHasNoAssociatedFieldName, id);
        }

        public byte GetOrCreateIdForField(string fieldName)
        {
            if (this.fieldToIdLookup.TryGetValue(fieldName, out var id))
            {
                return id;
            }

            var newId = Interlocked.Increment(ref nextId);
            if (newId > byte.MaxValue)
            {
                throw new LiftiException(ExceptionMessages.MaximumDistinctFieldsIndexReached);
            }

            id = (byte)newId;
            this.fieldToIdLookup[fieldName] = (byte)id;
            this.idToFieldLookup[id] = fieldName;
            return id;
        }
    }
}
