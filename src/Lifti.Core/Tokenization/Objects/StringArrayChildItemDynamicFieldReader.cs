using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;

namespace Lifti.Tokenization.Objects
{
    internal class StringArrayChildItemDynamicFieldReader<TItem, TChildItem> : ChildItemDynamicFieldReader<TItem, TChildItem, IEnumerable<string>>
    {
        public StringArrayChildItemDynamicFieldReader(
            Func<TItem, ICollection<TChildItem>?> getChildObjects,
            Func<TChildItem, string> getFieldName,
            Func<TChildItem, IEnumerable<string>> getFieldText,
            string? fieldNamePrefix,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            Thesaurus thesaurus)
            : base(getChildObjects, getFieldName, getFieldText, fieldNamePrefix, tokenizer, textExtractor, thesaurus)
        {
        }

        protected override IEnumerable<string> ReadFieldValueAsEnumerable(IEnumerable<string> fieldValue)
        {
            return fieldValue;
        }
    }
}