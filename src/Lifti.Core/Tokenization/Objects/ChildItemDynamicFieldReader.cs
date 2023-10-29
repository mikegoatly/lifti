using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    internal abstract class ChildItemDynamicFieldReader<TItem, TChildItem, TValue> : DynamicFieldReader<TItem>
    {
        private readonly Func<TItem, ICollection<TChildItem>?> getChildObjects;
        private readonly Func<TChildItem, string> getFieldName;
        private readonly Func<TChildItem, TValue> getFieldText;

        protected ChildItemDynamicFieldReader(
            Func<TItem, ICollection<TChildItem>?> getChildObjects,
            Func<TChildItem, string> getFieldName,
            Func<TChildItem, TValue> getFieldText,
            string dynamicFieldReaderName,
            string? fieldNamePrefix,
            IIndexTokenizer tokenizer,
            ITextExtractor textExtractor,
            Thesaurus thesaurus,
            double scoreBoost)
            : base(tokenizer, textExtractor, thesaurus, dynamicFieldReaderName, fieldNamePrefix, scoreBoost)
        {
            this.getChildObjects = getChildObjects;
            this.getFieldName = getFieldName;
            this.getFieldText = getFieldText;
        }

        public override ValueTask<IEnumerable<(string field, IEnumerable<string> rawText)>> ReadAsync(TItem item, CancellationToken cancellationToken)
        {
            var childObjects = this.getChildObjects(item);
            if (childObjects == null)
            {
                return EmptyFieldSet();
            }

            return new ValueTask<IEnumerable<(string, IEnumerable<string>)>>(
                childObjects
                    .Select(x => (this.GetPrefixedFieldName(this.getFieldName(x)), this.ReadFieldValueAsEnumerable(this.getFieldText(x))))
                    .ToList());
        }

        public override ValueTask<IEnumerable<string>> ReadAsync(TItem item, string fieldName, CancellationToken cancellationToken)
        {
            var unprefixedFieldName = this.GetUnprefixedFieldName(fieldName);

            var childObjects = this.getChildObjects(item);
            if (childObjects == null)
            {
                return EmptyField();
            }

            var childObject = childObjects.FirstOrDefault(x => this.getFieldName(x) == unprefixedFieldName);

            if (childObject == null)
            {
                // The field isn't known on this instance 
                return EmptyField();
            }

            return new ValueTask<IEnumerable<string>>(this.ReadFieldValueAsEnumerable(this.getFieldText(childObject)));
        }

        protected abstract IEnumerable<string> ReadFieldValueAsEnumerable(TValue fieldValue);
    }
}