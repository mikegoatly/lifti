using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    internal abstract class DictionaryDynamicFieldReader<TItem, TValue> : DynamicFieldReader<TItem>
    {
        private readonly Func<TItem, IDictionary<string, TValue>?> reader;

        public DictionaryDynamicFieldReader(
            Func<TItem, IDictionary<string, TValue>?> reader,
            string dynamicFieldReaderName,
            string? fieldNamePrefix,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            IThesaurus thesaurus)
            : base(tokenizer, textExtractor, thesaurus, dynamicFieldReaderName, fieldNamePrefix)
        {
            this.reader = reader;
        }

        /// <inheritdoc />
        public override ValueTask<IEnumerable<(string field, IEnumerable<string> rawText)>> ReadAsync(TItem item, CancellationToken cancellationToken)
        {
            var fields = this.reader(item);
            if (fields == null)
            {
                return EmptyFieldSet();
            }

            var results = new List<(string field, IEnumerable<string> rawText)>();

            foreach (var field in fields)
            {
                var fieldName = this.GetPrefixedFieldName(field.Key);

                results.Add((fieldName, this.ReadFieldValueAsEnumerable(field.Value)));
            }

            return new ValueTask<IEnumerable<(string, IEnumerable<string>)>>(results);
        }

        /// <inheritdoc />
        public override ValueTask<IEnumerable<string>> ReadAsync(TItem item, string fieldName, CancellationToken cancellationToken)
        {
            var unprefixedName = this.GetUnprefixedFieldName(fieldName);

            var fields = this.reader(item);
            if (fields != null && fields.TryGetValue(unprefixedName, out var field))
            {
                return new ValueTask<IEnumerable<string>>(this.ReadFieldValueAsEnumerable(field));
            }

            // The field is known to this reader, but not present for the given item instance.
            return DynamicFieldReader<TItem>.EmptyField();
        }

        protected abstract IEnumerable<string> ReadFieldValueAsEnumerable(TValue field);
    }
}