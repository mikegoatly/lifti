using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    internal abstract class ChildItemDynamicFieldReader<TObject, TChildObject, TValue> : DynamicFieldReader<TObject>
    {
        private readonly Func<TObject, ICollection<TChildObject>?> getChildObjects;
        private readonly Func<TChildObject, string> getFieldName;
        private readonly Func<TChildObject, TValue> getFieldText;

        protected ChildItemDynamicFieldReader(
            Func<TObject, ICollection<TChildObject>?> getChildObjects,
            Func<TChildObject, string> getFieldName,
            Func<TChildObject, TValue> getFieldText,
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

        public override ValueTask<IEnumerable<(string field, IEnumerable<ReadOnlyMemory<char>> rawText)>> ReadAsync(TObject item, CancellationToken cancellationToken)
        {
            var childObjects = this.getChildObjects(item);
            if (childObjects == null)
            {
                return EmptyFieldSet();
            }

            return new ValueTask<IEnumerable<(string, IEnumerable<ReadOnlyMemory<char>>)>>(
                childObjects
                    .Select(x => (this.GetPrefixedFieldName(this.getFieldName(x)), this.ReadFieldValueAsEnumerable(this.getFieldText(x))))
                    .ToList());
        }

        public override ValueTask<IEnumerable<ReadOnlyMemory<char>>> ReadAsync(TObject item, string fieldName, CancellationToken cancellationToken)
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

            return new ValueTask<IEnumerable<ReadOnlyMemory<char>>>(this.ReadFieldValueAsEnumerable(this.getFieldText(childObject)));
        }

        protected abstract IEnumerable<ReadOnlyMemory<char>> ReadFieldValueAsEnumerable(TValue fieldValue);
    }
}