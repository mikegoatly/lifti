using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;

namespace Lifti.Tokenization.Objects
{
    internal class StringArrayDictionaryDynamicFieldReader<TItem> : DictionaryDynamicFieldReader<TItem, IEnumerable<string>>
    {
        public StringArrayDictionaryDynamicFieldReader(
            Func<TItem, IDictionary<string, IEnumerable<string>>> reader,
            string? fieldNamePrefix,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            IThesaurus thesaurus)
            : base(reader, fieldNamePrefix, tokenizer, textExtractor, thesaurus)
        {
        }

        protected override IEnumerable<string> ReadFieldValueAsEnumerable(IEnumerable<string> field)
        {
            return field;
        }
    }
}