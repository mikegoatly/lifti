using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;

namespace Lifti.Tokenization.Objects
{
    internal sealed class StringChildObjectDynamicFieldReader<TItem, TChildItem> : ChildItemDynamicFieldReader<TItem, TChildItem, ReadOnlyMemory<char>>
    {
        public StringChildObjectDynamicFieldReader(
            Func<TItem, ICollection<TChildItem>?> getChildObjects,
            Func<TChildItem, string> getFieldName,
            Func<TChildItem, ReadOnlyMemory<char>> getFieldText,
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

        protected override IEnumerable<ReadOnlyMemory<char>> ReadFieldValueAsEnumerable(ReadOnlyMemory<char> fieldValue)
        {
            return new[] { fieldValue };
        }
    }
}