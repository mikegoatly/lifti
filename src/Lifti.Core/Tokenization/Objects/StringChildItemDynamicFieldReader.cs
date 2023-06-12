using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;

namespace Lifti.Tokenization.Objects
{
    internal class StringChildItemDynamicFieldReader<TItem, TChildItem> : ChildItemDynamicFieldReader<TItem, TChildItem, string>
    {
        public StringChildItemDynamicFieldReader(
            Func<TItem, ICollection<TChildItem>?> getChildObjects,
            Func<TChildItem, string> getFieldName,
            Func<TChildItem, string> getFieldText,
            string? fieldNamePrefix,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            Thesaurus thesaurus)
            : base(getChildObjects, getFieldName, getFieldText, fieldNamePrefix, tokenizer, textExtractor, thesaurus)
        {
        }

        protected override IEnumerable<string> ReadFieldValueAsEnumerable(string fieldValue)
        {
            return new[] { fieldValue };
        }
    }
}