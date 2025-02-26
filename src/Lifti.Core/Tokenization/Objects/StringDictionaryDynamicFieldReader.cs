using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;

namespace Lifti.Tokenization.Objects
{
    internal sealed class StringDictionaryDynamicFieldReader<TObject> : DictionaryDynamicFieldReader<TObject, string>
    {
        public StringDictionaryDynamicFieldReader(
            Func<TObject, IDictionary<string, string>?> reader,
            string dynamicFieldReaderName,
            string? fieldNamePrefix,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            IThesaurus thesaurus,
            double scoreBoost)
            : base(reader, dynamicFieldReaderName, fieldNamePrefix, tokenizer, textExtractor, thesaurus, scoreBoost)
        {
        }

        protected override IEnumerable<ReadOnlyMemory<char>> ReadFieldValueAsEnumerable(string fieldValue)
        {
            return [fieldValue.AsMemory()];
        }
    }
}