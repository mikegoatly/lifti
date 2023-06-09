using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    internal class DictionaryDynamicFieldReader<TItem> : DynamicFieldReader<TItem>
    {
        private readonly Func<TItem, IEnumerable<KeyValuePair<string, string>>> reader;
        private readonly string? fieldNamePrefix;

        public DictionaryDynamicFieldReader(
            Func<TItem, ICollection<KeyValuePair<string, string>>> reader,
            string? fieldNamePrefix,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            IThesaurus thesaurus)
            : base(tokenizer, textExtractor, thesaurus)
        {
            this.reader = reader;
            this.fieldNamePrefix = fieldNamePrefix;
        }

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
    }
}