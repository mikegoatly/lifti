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
        private readonly Dictionary<string, IndexedFieldDetails> fieldToDetailsLookup = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// This allows us to create a dynamic field at runtime while only knowing the name of the dynamic field reader.
        /// In this situation we won't know the associated item type and we can avoid runtime reflection.
        /// </summary>
        private readonly Dictionary<string, Func<string, IndexedFieldDetails>> dynamicFieldFactoryLookup = new();

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
            return this.fieldToDetailsLookup.TryGetValue(fieldName, out var fieldDetails) && fieldDetails.ObjectType == objectType;
        }

        internal void RegisterDynamicFieldReader<TItem>(DynamicFieldReader<TItem> reader)
        {
            if (this.dynamicFieldFactoryLookup.ContainsKey(reader.Name))
            {
                throw new LiftiException(ExceptionMessages.DuplicateDynamicFieldReaderName, reader.Name);
            }

            this.dynamicFieldFactoryLookup[reader.Name] = fieldName => this.GetOrCreateDynamicFieldInfo(reader, fieldName);
        }

        internal void RegisterStaticField<TItem>(IStaticFieldReader<TItem> reader)
        {
            this.RegisterField(
                reader.Name,
                (name, id) => IndexedFieldDetails<TItem>.Static(
                    id,
                    name,
                    reader.ReadAsync,
                    reader.TextExtractor,
                    reader.Tokenizer,
                    reader.Thesaurus));
        }

        internal IndexedFieldDetails GetOrCreateDynamicFieldInfo(string dynamicFieldReaderName, string fieldName)
        {
            if (this.dynamicFieldFactoryLookup.TryGetValue(dynamicFieldReaderName, out var factory))
            {
                return factory(fieldName);
            }

            throw new LiftiException(ExceptionMessages.UnknownDynamicFieldReaderNameEncountered, dynamicFieldReaderName);
        }

        internal IndexedFieldDetails GetOrCreateDynamicFieldInfo<TItem>(DynamicFieldReader<TItem> fieldReader, string fieldName)
        {
            if (!this.fieldToDetailsLookup.TryGetValue(fieldName, out var details))
            {
                details = this.RegisterField(
                    fieldName,
                    (name, id) => IndexedFieldDetails<TItem>.Dynamic(
                        id,
                        name,
                        fieldReader.Name,
                        (item, cancellationToken) => fieldReader.ReadAsync(item, fieldName, cancellationToken),
                        fieldReader.TextExtractor,
                        fieldReader.Tokenizer,
                        fieldReader.Thesaurus));
            }
            else
            {
                if (details.FieldKind != FieldKind.Dynamic)
                {
                    // We can't allow an index to have a static field registered, and then a dynamic field is registered with the same name.
                    throw new LiftiException(ExceptionMessages.CannotRegisterDynamicFieldWithSameNameAsStaticField, fieldName);
                }

                if (details.ObjectType != typeof(TItem))
                {
                    // Field was previously registered with 
                    throw new LiftiException(ExceptionMessages.CannotRegisterDynamicFieldWithSameNameForTwoDifferentObjectTypes, fieldName);
                }
            }

            return details;
        }

        private IndexedFieldDetails<TItem> RegisterField<TItem>(
            string fieldName,
            Func<string, byte, IndexedFieldDetails<TItem>> createFieldDetails)
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
            var details = createFieldDetails(fieldName, id);

            this.fieldToDetailsLookup[fieldName] = details;

            this.idToFieldLookup[id] = fieldName;

            return details;
        }
    }
}
