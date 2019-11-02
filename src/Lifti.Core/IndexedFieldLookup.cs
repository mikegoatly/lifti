using Lifti.ItemTokenization;
using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Lifti
{
    public class IndexedFieldLookup : IIndexedFieldLookup
    {
        private readonly Dictionary<string, IndexedFieldDetails> fieldToDetailsLookup = new Dictionary<string, IndexedFieldDetails>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<byte, string> idToFieldLookup = new Dictionary<byte, string>();
        private int nextId = 0;

        internal IndexedFieldLookup(
            IEnumerable<IFieldTokenizationOptions> fieldTokenizationOptions, 
            ITokenizerFactory tokenizerFactory,
            TokenizationOptions defaultTokenizationOptions)
        {
            if (fieldTokenizationOptions is null)
            {
                throw new ArgumentNullException(nameof(fieldTokenizationOptions));
            }

            if (tokenizerFactory is null)
            {
                throw new ArgumentNullException(nameof(tokenizerFactory));
            }

            if (defaultTokenizationOptions is null)
            {
                throw new ArgumentNullException(nameof(defaultTokenizationOptions));
            }

            foreach (var field in fieldTokenizationOptions)
            {
                this.RegisterField(field, tokenizerFactory, defaultTokenizationOptions);
            }
        }

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

        public IndexedFieldDetails GetFieldInfo(string fieldName)
        {
            if (!this.fieldToDetailsLookup.TryGetValue(fieldName, out var details))
            {
                throw new LiftiException(ExceptionMessages.UnknownField, fieldName);
            }

            return details;
        }

        private void RegisterField(IFieldTokenizationOptions fieldOptions, ITokenizerFactory tokenizerFactory, TokenizationOptions defaultTokenizationOptions)
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
            var fieldTokenizationOptions = fieldOptions.TokenizationOptions ?? defaultTokenizationOptions;
            this.fieldToDetailsLookup[fieldName] = new IndexedFieldDetails((byte)id, tokenizerFactory.Create(fieldTokenizationOptions));
            this.idToFieldLookup[id] = fieldName;
        }
    }
}
