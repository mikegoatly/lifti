using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tokenization.Objects
{
    internal sealed class StringArrayDictionaryDynamicFieldReader<TObject> : DictionaryDynamicFieldReader<TObject, IEnumerable<string>>
    {
        public StringArrayDictionaryDynamicFieldReader(
            string dynamicFieldReaderName,
            Func<TObject, IDictionary<string, IEnumerable<string>>> reader,
            string? fieldNamePrefix,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            IThesaurus thesaurus,
            double scoreBoost)
            : base(
                  reader,
                  dynamicFieldReaderName,
                  fieldNamePrefix,
                  tokenizer,
                  textExtractor,
                  thesaurus,
                  scoreBoost)
        {
        }

        protected override IEnumerable<ReadOnlyMemory<char>> ReadFieldValueAsEnumerable(IEnumerable<string> field)
        {
            return field.Select(x => x.AsMemory());
        }
    }
}