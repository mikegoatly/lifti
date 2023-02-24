using Lifti.Tokenization.Objects;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Lifti
{
    /// <inheritdoc />
    internal class IndexedFieldLookup : IIndexedFieldLookup
    {
        internal const string DefaultFieldName = "Unspecified";

        private readonly Dictionary<string, IndexedFieldDetails> fieldToDetailsLookup = new Dictionary<string, IndexedFieldDetails>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<byte, string> idToFieldLookup = new Dictionary<byte, string>();
        private int nextId;

        internal IndexedFieldLookup(IEnumerable<IFieldReader> fieldReaders)
        {
            if (fieldReaders is null)
            {
                throw new ArgumentNullException(nameof(fieldReaders));
            }

            foreach (var field in fieldReaders)
            {
                this.RegisterField(field);
            }
        }

        /// <inheritdoc />
        public byte DefaultField { get; }

        /// <inheritdoc />
        public string GetFieldForId(byte id)
        {
            if (id == 0)
            {
                return DefaultFieldName;
            }
            else if (idToFieldLookup.TryGetValue(id, out var fieldName))
            {
                return fieldName;
            }

            throw new LiftiException(ExceptionMessages.FieldHasNoAssociatedFieldName, id);
        }

        /// <inheritdoc />
        public IndexedFieldDetails GetFieldInfo(string fieldName)
        {
            if (!this.fieldToDetailsLookup.TryGetValue(fieldName, out var details))
            {
                throw new LiftiException(ExceptionMessages.UnknownField, fieldName);
            }

            return details;
        }

        private void RegisterField(IFieldReader fieldOptions)
        {
            var fieldName = fieldOptions.Name;
            if (this.fieldToDetailsLookup.ContainsKey(fieldOptions.Name))
            {
                throw new LiftiException(ExceptionMessages.FieldNameAlreadyUsed, fieldName);
            }

            var newId = Interlocked.Increment(ref nextId);
            if (newId > byte.MaxValue)
            {
                throw new LiftiException(ExceptionMessages.MaximumDistinctFieldsIndexReached);
            }

            var id = (byte)newId;
            this.fieldToDetailsLookup[fieldName] = new IndexedFieldDetails(
                id,
                fieldOptions.TextExtractor,
                fieldOptions.Tokenizer,
                fieldOptions.Thesaurus);

            this.idToFieldLookup[id] = fieldName;
        }
    }
}
