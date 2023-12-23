using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;

namespace Lifti.Tokenization.Objects
{
    internal class StringChildObjectDynamicFieldReader<TItem, TChildItem> : ChildItemDynamicFieldReader<TItem, TChildItem, string>
    {
        public StringChildObjectDynamicFieldReader(
            Func<TItem, ICollection<TChildItem>?> getChildObjects,
            Func<TChildItem, string> getFieldName,
            Func<TChildItem, string> getFieldText,
            string dynamicFieldReaderName,
            string? fieldNamePrefix,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            Thesaurus thesaurus,
            double scoreBoost)
            : base(getChildObjects,
                   getFieldName,
                   getFieldText,
                   dynamicFieldReaderName,
                   fieldNamePrefix,
                   tokenizer,
                   textExtractor,
                   thesaurus,
                   scoreBoost)
        {
        }

        protected override IEnumerable<string> ReadFieldValueAsEnumerable(string fieldValue)
        {
            return new[] { fieldValue };
        }
    }
}