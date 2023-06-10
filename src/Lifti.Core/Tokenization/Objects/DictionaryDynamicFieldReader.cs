using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    // TODO Test
    internal class DictionaryDynamicFieldReader<TItem> : DynamicFieldReader<TItem>
    {
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
                var fieldName = this.fieldNamePrefix == null ? field.Key : $"{this.fieldNamePrefix}{field.Key}";
                results.Add((fieldName, field.Value));
            }

            return new ValueTask<IEnumerable<(string, string)>>(results);
        }

        /// <inheritdoc />
        public override ValueTask<IEnumerable<string>> ReadAsync(TItem item, string fieldName, CancellationToken cancellationToken)
        {
            var fields = this.reader(item);
            if (fields.TryGetValue(fieldName, out var field))
            {
                return new ValueTask<IEnumerable<string>>(new[] { field });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}