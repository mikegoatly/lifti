using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tests.Fakes
{
    public class FakeIndexedFieldLookup : IIndexedFieldLookup
    {
        private Dictionary<string, IndexedFieldDetails> fieldLookup;

        public FakeIndexedFieldLookup(params (string fieldName, IndexedFieldDetails details)[] fields)
        {
            this.fieldLookup = fields.ToDictionary(f => f.fieldName, f => f.details);
        }

        public byte DefaultField => 0;

        public IReadOnlyCollection<string> AllFieldNames => fieldLookup.Keys;

        public string GetFieldForId(byte id)
        {
            return fieldLookup.Single(f => f.Value.Id == id).Key;
        }

        public IndexedFieldDetails GetFieldInfo(string fieldName)
        {
            return fieldLookup[fieldName];
        }

        public bool IsKnownField(Type objectType, string fieldName)
        {
            return fieldLookup.ContainsKey(fieldName);
        }
    }
}
