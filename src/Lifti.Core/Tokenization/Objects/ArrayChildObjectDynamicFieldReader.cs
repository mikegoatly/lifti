using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;

namespace Lifti.Tokenization.Objects
{
    internal sealed class ArrayChildObjectDynamicFieldReader<TItem, TChildItem> : ChildItemDynamicFieldReader<TItem, TChildItem, IEnumerable<ReadOnlyMemory<char>>>
    {
        internal ArrayChildObjectDynamicFieldReader(
            Func<TItem, ICollection<TChildItem>?> getChildObjects,
            Func<TChildItem, string> getFieldName,
            Func<TChildItem, IEnumerable<ReadOnlyMemory<char>>> getFieldText,
            string dynamicFieldReaderName,
            string? fieldNamePrefix,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            Thesaurus thesaurus,
            double scoreBoost)
            : base(
                  getChildObjects,
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

        protected override IEnumerable<ReadOnlyMemory<char>> ReadFieldValueAsEnumerable(IEnumerable<ReadOnlyMemory<char>> fieldValue)
        {
            return fieldValue;
        }
    }
}