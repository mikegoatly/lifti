using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    /// <summary>
    /// Implemented by classes that can read an object's fields dynamically during indexing.
    /// </summary>
    internal abstract class DynamicFieldReader<TItem> : FieldConfig
    {
        private readonly Dictionary<string, string> prefixedFields = new();
        private readonly Dictionary<string, string> prefixedFieldsReverseLookup = new();
        private readonly string? fieldNamePrefix;

        protected DynamicFieldReader(IIndexTokenizer tokenizer, ITextExtractor textExtractor, IThesaurus thesaurus, string? fieldNamePrefix)
            : base(tokenizer, textExtractor, thesaurus)
        {
            this.fieldNamePrefix = fieldNamePrefix;
        }

        /// <summary>
        /// Provides a delegate capable of reading all fields and associated text from an object.
        /// </summary>
        public abstract ValueTask<IEnumerable<(string field, IEnumerable<string> rawText)>> ReadAsync(TItem item, CancellationToken cancellationToken);

        /// <summary>
        /// Provides a delegate capable of reading a specific dynamic field from an object. If the field is not found on the given
        /// object, an empty enumerable will be returned and no error thrown.
        /// </summary>
        public abstract ValueTask<IEnumerable<string>> ReadAsync(TItem item, string fieldName, CancellationToken cancellationToken);

        protected string GetPrefixedFieldName(string unprefixedFieldName)
        {
            if (!this.prefixedFields.TryGetValue(unprefixedFieldName, out var fieldName))
            {
                fieldName = this.fieldNamePrefix == null ? unprefixedFieldName : $"{this.fieldNamePrefix}{unprefixedFieldName}";

                // Keying the fieldname against its prefixed version in both directions allows for quick lookups later on without string manipulation
                this.prefixedFields[unprefixedFieldName] = fieldName;
                this.prefixedFieldsReverseLookup[fieldName] = unprefixedFieldName;
            }

            return fieldName;
        }

        protected string GetUnprefixedFieldName(string prefixedFieldName)
        {
            if (this.prefixedFieldsReverseLookup.TryGetValue(prefixedFieldName, out var unprefixedName) == false)
            {
                // Field is not known against this object.
                throw new LiftiException(ExceptionMessages.AttemptToReadFieldUnknownToDynamicFieldReader, prefixedFieldName);
            }

            return unprefixedName;
        }

        protected static ValueTask<IEnumerable<string>> EmptyField()
        {
            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        protected static ValueTask<IEnumerable<(string field, IEnumerable<string> rawText)>> EmptyFieldSet()
        {
            return new ValueTask<IEnumerable<(string field, IEnumerable<string> rawText)>>(Array.Empty<(string, IEnumerable<string>)>());
        }
    }
}