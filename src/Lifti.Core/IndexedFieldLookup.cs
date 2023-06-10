using Lifti.Tokenization.Objects;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti
{
    /// <inheritdoc />
    internal class IndexedFieldLookup : IIndexedFieldLookup
    {
        internal const string DefaultFieldName = "Unspecified";
        private readonly Dictionary<string, IndexedFieldDetails> fieldToDetailsLookup = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<byte, string> idToFieldLookup = new();
        private int nextId;


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

        /// <inheritdoc/>

        public bool IsKnownField(Type objectType, string fieldName)
        {
            return this.fieldToDetailsLookup.ContainsKey(fieldName);
        }

        internal void RegisterStaticField<TItem>(IStaticFieldReader<TItem> reader)
        {
            this.RegisterField<TItem>(reader.ReadAsync, true, reader.Name, reader);
        }

        internal IndexedFieldDetails GetOrCreateDynamicFieldInfo<TItem>(DynamicFieldReader<TItem> fieldReader, string fieldName)
        {
            if (!this.fieldToDetailsLookup.TryGetValue(fieldName, out var details))
            {
                details = this.RegisterField<TItem>(
                    (item, cancellationToken) => fieldReader.ReadAsync(item, fieldName, cancellationToken),
                    true,
                    fieldName,
                    fieldReader);
            }
            else
            {
                if (details.FieldKind != FieldKind.Dynamic)
                {
                    // We can't allow an index to have a static field registered, and then a dynamic field is registered with the same name.
                    // TODO test
                    throw new LiftiException(ExceptionMessages.CannotRegisterDynamicFieldWithSameNameAsStaticField, fieldName);
                }
            }

            return details;
        }

        private IndexedFieldDetails<TItem> RegisterField<TItem>(Func<TItem, CancellationToken, ValueTask<IEnumerable<string>>> fieldReader, bool isDynamicField, string fieldName, IFieldConfig fieldConfig)
        {
            if (this.fieldToDetailsLookup.ContainsKey(fieldName))
            {
                throw new LiftiException(ExceptionMessages.FieldNameAlreadyUsed, fieldName);
            }

            var newId = Interlocked.Increment(ref this.nextId);
            if (newId > byte.MaxValue)
            {
                throw new LiftiException(ExceptionMessages.MaximumDistinctFieldsIndexReached);
            }

            var id = (byte)newId;
            var details = new IndexedFieldDetails<TItem>(
                id,
                fieldReader,
                isDynamicField ? FieldKind.Dynamic : FieldKind.Static,
                fieldConfig.TextExtractor,
                fieldConfig.Tokenizer,
                fieldConfig.Thesaurus);

            this.fieldToDetailsLookup[fieldName] = details;

            this.idToFieldLookup[id] = fieldName;

            return details;
        }
    }
}
