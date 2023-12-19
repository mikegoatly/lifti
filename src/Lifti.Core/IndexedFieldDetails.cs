using Lifti.Tokenization;
using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti
{

    /// <summary>
    /// Information about a field that has been configured for indexing.
    /// </summary>
    public abstract class IndexedFieldDetails
    {
        internal IndexedFieldDetails(
            byte id,
            string name,
            Type objectType,
            FieldKind fieldKind,
            ITextExtractor textExtractor,
            IIndexTokenizer tokenizer,
            IThesaurus thesaurus,
            string? dynamicFieldReaderName,
            double scoreBoost)
        {
            this.Id = id;
            this.Name = name;
            this.ObjectType = objectType;
            this.FieldKind = fieldKind;
            this.TextExtractor = textExtractor;
            this.Tokenizer = tokenizer;
            this.Thesaurus = thesaurus;
            this.DynamicFieldReaderName = dynamicFieldReaderName;
            this.ScoreBoost = scoreBoost;
        }

        /// <summary>
        /// Gets the id of the field.
        /// </summary>
        public byte Id { get; }

        /// <summary>
        /// Gets the name of the field.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the object the field is registered for.
        /// </summary>
        public Type ObjectType { get; }

        /// <summary>
        /// Gets the kind of field this instance represents.
        /// </summary>
        public FieldKind FieldKind { get; }

        /// <summary>
        /// Gets the <see cref="ITextExtractor"/> used to extract sections of text from this field.
        /// </summary>
        public ITextExtractor TextExtractor { get; }

        /// <summary>
        /// Gets the <see cref="IIndexTokenizer"/> that should be used when tokenizing text for the field.
        /// </summary>
        public IIndexTokenizer Tokenizer { get; }

        /// <summary>
        /// Gets the <see cref="IThesaurus"/> that should be used to expand tokens when processing text for this field.
        /// </summary>
        public IThesaurus Thesaurus { get; }

        /// <summary>
        /// Gets the name of the dynamic field reader that generated this field. If this field is not a dynamic field, this will be null.
        /// </summary>
        public string? DynamicFieldReaderName { get; }

        /// <summary>
        /// Gets the score boost to apply to this field.
        /// </summary>
        public double ScoreBoost { get; }

        /// <summary>
        /// Reads the text for the field from the specified item. The item must be of the type specified by the <see cref="ObjectType"/> property.
        /// </summary>
        public abstract ValueTask<IEnumerable<string>> ReadAsync(object item, CancellationToken cancellationToken);

        internal void Deconstruct(out byte fieldId, out ITextExtractor textExtractor, out IIndexTokenizer tokenizer, out IThesaurus thesaurus)
        {
            fieldId = this.Id;
            tokenizer = this.Tokenizer;
            textExtractor = this.TextExtractor;
            thesaurus = this.Thesaurus;
        }
    }

    /// <inheritdoc />
    public class IndexedFieldDetails<TObject> : IndexedFieldDetails
    {
        private readonly Func<TObject, CancellationToken, ValueTask<IEnumerable<string>>> fieldReader;

        private IndexedFieldDetails(
            byte id,
            string name,
            Func<TObject, CancellationToken, ValueTask<IEnumerable<string>>> fieldReader,
            FieldKind fieldKind,
            ITextExtractor textExtractor,
            IIndexTokenizer tokenizer,
            IThesaurus thesaurus,
            string? dynamicFieldReaderName,
            double scoreBoost)
            : base(id, name, typeof(TObject), fieldKind, textExtractor, tokenizer, thesaurus, dynamicFieldReaderName, scoreBoost)
        {
            this.fieldReader = fieldReader;
        }

        internal static IndexedFieldDetails<TObject> Static(byte id,
            string name,
            Func<TObject, CancellationToken, ValueTask<IEnumerable<string>>> fieldReader,
            ITextExtractor textExtractor,
            IIndexTokenizer tokenizer,
            IThesaurus thesaurus,
            double scoreBoost)
        {
            return new IndexedFieldDetails<TObject>(
                id,
                name,
                fieldReader,
                FieldKind.Static,
                textExtractor,
                tokenizer,
                thesaurus,
                null,
                scoreBoost);
        }

        internal static IndexedFieldDetails<TObject> Dynamic(byte id,
            string name,
            string dynamicFieldReaderName,
            Func<TObject, CancellationToken, ValueTask<IEnumerable<string>>> fieldReader,
            ITextExtractor textExtractor,
            IIndexTokenizer tokenizer,
            IThesaurus thesaurus,
            double scoreBoost)
        {
            return new IndexedFieldDetails<TObject>(
                id,
                name,
                fieldReader,
                FieldKind.Dynamic,
                textExtractor,
                tokenizer,
                thesaurus,
                dynamicFieldReaderName,
                scoreBoost);
        }

        /// <inheritdoc />
        public override ValueTask<IEnumerable<string>> ReadAsync(object item, CancellationToken cancellationToken)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (item is TObject typedItem)
            {
                return this.fieldReader(typedItem, cancellationToken);
            }

            throw new ArgumentException($"Item type {item.GetType().Name} is not expected type {this.ObjectType.Name}");

        }
    }
}
