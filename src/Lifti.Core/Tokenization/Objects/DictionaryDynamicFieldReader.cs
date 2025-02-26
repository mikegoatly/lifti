using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    internal abstract class DictionaryDynamicFieldReader<TObject, TValue> : DynamicFieldReader<TObject>
    {
        private readonly Func<TObject, IDictionary<string, TValue>?> reader;

        public DictionaryDynamicFieldReader(
            Func<TObject, IDictionary<string, TValue>?> reader,
            string dynamicFieldReaderName,
            string? fieldNamePrefix,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            IThesaurus thesaurus,
            double scoreBoost)
            : base(tokenizer, textExtractor, thesaurus, dynamicFieldReaderName, fieldNamePrefix, scoreBoost)
        {
            this.reader = reader;
        }

        /// <inheritdoc />
        public override ValueTask<IEnumerable<(string field, IEnumerable<ReadOnlyMemory<char>> rawText)>> ReadAsync(TObject item, CancellationToken cancellationToken)
        {
            var fields = this.reader(item);
            if (fields == null)
            {
                return EmptyFieldSet();
            }

            var results = new List<(string field, IEnumerable<ReadOnlyMemory<char>> rawText)>();

            foreach (var field in fields)
            {
                var fieldName = this.GetPrefixedFieldName(field.Key);

                results.Add((fieldName, this.ReadFieldValueAsEnumerable(field.Value)));
            }

            return new ValueTask<IEnumerable<(string, IEnumerable<ReadOnlyMemory<char>>)>>(results);
        }

        /// <inheritdoc />
        public override ValueTask<IEnumerable<ReadOnlyMemory<char>>> ReadAsync(TObject item, string fieldName, CancellationToken cancellationToken)
        {
            var unprefixedName = this.GetUnprefixedFieldName(fieldName);

            var fields = this.reader(item);
            if (fields != null && fields.TryGetValue(unprefixedName, out var field))
            {
                return new ValueTask<IEnumerable<ReadOnlyMemory<char>>>(this.ReadFieldValueAsEnumerable(field));
            }

            // The field is known to this reader, but not present for the given instance.
            return EmptyField();
        }

        protected abstract IEnumerable<ReadOnlyMemory<char>> ReadFieldValueAsEnumerable(TValue field);
    }
}