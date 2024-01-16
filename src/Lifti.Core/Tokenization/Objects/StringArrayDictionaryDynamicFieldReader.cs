using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;

namespace Lifti.Tokenization.Objects
{
    internal class StringArrayDictionaryDynamicFieldReader<TObject> : DictionaryDynamicFieldReader<TObject, IEnumerable<string>>
    {
        public StringArrayDictionaryDynamicFieldReader(
            string dynamicFieldReaderName,
            Func<TObject, IDictionary<string, IEnumerable<string>>> reader,
            string? fieldNamePrefix,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            IThesaurus thesaurus,
            double scoreBoost)
            : base(reader, dynamicFieldReaderName, fieldNamePrefix, tokenizer, textExtractor, thesaurus, scoreBoost)
        {
        }

        protected override IEnumerable<string> ReadFieldValueAsEnumerable(IEnumerable<string> field)
        {
            return field;
        }
    }
}