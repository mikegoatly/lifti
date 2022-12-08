using Lifti.Tokenization;
using Lifti.Tokenization.Objects;
using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Lifti
{
    /// <inheritdoc />
    public class IndexedFieldLookup : IIndexedFieldLookup
    {
        internal const string DefaultFieldName = "Unspecified";

        private readonly Dictionary<string, IndexedFieldDetails> fieldToDetailsLookup = new Dictionary<string, IndexedFieldDetails>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<byte, string> idToFieldLookup = new Dictionary<byte, string>();
        private int nextId = 0;

        internal IndexedFieldLookup(
            IEnumerable<IFieldReader> fieldReaders, 
            ITextExtractor defaultTextExtractor,
            IIndexTokenizer defaultTokenizer)
        {
            if (fieldReaders is null)
            {
                throw new ArgumentNullException(nameof(fieldReaders));
            }

            if (defaultTextExtractor is null)
            {
                throw new ArgumentNullException(nameof(defaultTextExtractor));
            }

            if (defaultTokenizer is null)
            {
                throw new ArgumentNullException(nameof(defaultTokenizer));
            }

            foreach (var field in fieldReaders)
            {
                this.RegisterField(field, defaultTextExtractor, defaultTokenizer);
            }
        }

        /// <inheritdoc />
        public byte DefaultField { get; } = 0;

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

        private void RegisterField(IFieldReader fieldOptions, ITextExtractor defaultTextExtractor, IIndexTokenizer defaultTokenizer)
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
            var fieldTokenizer = fieldOptions.Tokenizer ?? defaultTokenizer;
            var textExtractor = fieldOptions.TextExtractor ?? defaultTextExtractor;
            this.fieldToDetailsLookup[fieldName] = new IndexedFieldDetails((byte)id, textExtractor, fieldTokenizer);
            this.idToFieldLookup[id] = fieldName;
        }
    }
}
