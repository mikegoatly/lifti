using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;

namespace Lifti.Tokenization.Objects
{
    internal class StringDictionaryDynamicFieldReader<TItem> : DictionaryDynamicFieldReader<TItem, string>
    {
        public StringDictionaryDynamicFieldReader(
            Func<TItem, IDictionary<string, string>?> reader,
            string dynamicFieldReaderName,
            string? fieldNamePrefix,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            IThesaurus thesaurus)
            : base(reader, dynamicFieldReaderName, fieldNamePrefix, tokenizer, textExtractor, thesaurus)
        {
        }

        protected override IEnumerable<string> ReadFieldValueAsEnumerable(string fieldValue)
        {
            return new[] { fieldValue };
        }
    }
}