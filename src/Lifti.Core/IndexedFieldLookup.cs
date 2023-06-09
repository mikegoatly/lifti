using Lifti.Tokenization.Objects;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Lifti
{
    /// <inheritdoc />
    public class IndexedFieldLookup : IIndexedFieldLookup
    {
        internal const string DefaultFieldName = "Unspecified";

        private readonly Dictionary<string, IndexedFieldDetails> fieldToDetailsLookup = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<byte, string> idToFieldLookup = new();
        private int nextId;

        internal IndexedFieldLookup(IEnumerable<IStaticFieldReader> fieldReaders)
        {
            if (fieldReaders is null)
            {
                throw new ArgumentNullException(nameof(fieldReaders));
            }

            foreach (var field in fieldReaders)
            {
                this.RegisterField(field.Name, field);
            }
        }

        /// <inheritdoc />
        public IReadOnlyCollection<string> AllFieldNames => this.fieldToDetailsLookup.Keys;

        /// <inheritdoc />
        public byte DefaultField { get; }

        /// <inheritdoc />
        public string GetFieldForId(byte id)
        {
            if (id == 0)
            {
                return DefaultFieldName;
            }
            else if (this.idToFieldLookup.TryGetValue(id, out var fieldName))
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

        internal IndexedFieldDetails GetOrCreateDynamicFieldInfo(string fieldName, IFieldConfig fieldConfig)
        {
            if (!this.fieldToDetailsLookup.TryGetValue(fieldName, out var details))
            {
                details = this.RegisterField(fieldName, fieldConfig);
            }

            return details;
        }

        private IndexedFieldDetails RegisterField(string name, IFieldConfig fieldConfig)
        {
            var fieldName = name;
            if (this.fieldToDetailsLookup.ContainsKey(name))
            {
                throw new LiftiException(ExceptionMessages.FieldNameAlreadyUsed, fieldName);
            }

            var newId = Interlocked.Increment(ref this.nextId);
            if (newId > byte.MaxValue)
            {
                throw new LiftiException(ExceptionMessages.MaximumDistinctFieldsIndexReached);
            }

            var id = (byte)newId;
            var details = new IndexedFieldDetails(
                id,
                fieldConfig.TextExtractor,
                fieldConfig.Tokenizer,
                fieldConfig.Thesaurus);

            this.fieldToDetailsLookup[fieldName] = details;

            this.idToFieldLookup[id] = fieldName;

            return details;
        }
    }
}
