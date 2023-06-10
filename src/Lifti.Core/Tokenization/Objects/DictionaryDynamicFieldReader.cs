using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    internal class DictionaryDynamicFieldReader<TItem> : DynamicFieldReader<TItem>
    {
        private readonly Dictionary<string, string> prefixedFields = new();
        private readonly Dictionary<string, string> prefixedFieldsReverseLookup = new();
        private readonly Func<TItem, IDictionary<string, string>> reader;
        private readonly string? fieldNamePrefix;

        public DictionaryDynamicFieldReader(
            Func<TItem, IDictionary<string, string>> reader,
            string? fieldNamePrefix,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            IThesaurus thesaurus)
            : base(tokenizer, textExtractor, thesaurus)
        {
            this.reader = reader;
            this.fieldNamePrefix = fieldNamePrefix;
        }

        /// <inheritdoc />
        public override ValueTask<IEnumerable<(string field, string rawText)>> ReadAsync(TItem item, CancellationToken cancellationToken)
        {
            var results = new List<(string field, string rawText)>();

            foreach (var field in this.reader(item))
            {
                if (!this.prefixedFields.TryGetValue(field.Key, out var fieldName))
                {
                    fieldName = this.fieldNamePrefix == null ? field.Key : $"{this.fieldNamePrefix}{field.Key}";

                    // Keying the fieldname against its prefixed version in both directions allows for quick lookups later on without string manipulation
                    this.prefixedFields[field.Key] = fieldName;
                    this.prefixedFieldsReverseLookup[fieldName] = field.Key;
                }

                results.Add((fieldName, field.Value));
            }

            return new ValueTask<IEnumerable<(string, string)>>(results);
        }

        /// <inheritdoc />
        public override ValueTask<IEnumerable<string>> ReadAsync(TItem item, string fieldName, CancellationToken cancellationToken)
        {
            if (this.prefixedFieldsReverseLookup.TryGetValue(fieldName, out var unprefixedName) == false)
            {
                // Field is not known against this object.
                throw new LiftiException(ExceptionMessages.AttemptToReadFieldUnknownToDynamicFieldReader, fieldName);
            }

            var fields = this.reader(item);
            if (fields.TryGetValue(unprefixedName, out var field))
            {
                return new ValueTask<IEnumerable<string>>(new[] { field });
            }

            // The field is known to this reader, but not present for the given item instance.
            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}