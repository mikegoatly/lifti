using Lifti.Tokenization.TextExtraction;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti.Tokenization.Objects
{
    /// <summary>
    /// Implemented by classes that can read an object's fields dynamically during indexing.
    /// </summary>
    internal abstract class DynamicFieldReader<TItem> : FieldConfig
    {
        protected DynamicFieldReader(IIndexTokenizer tokenizer, ITextExtractor textExtractor, IThesaurus thesaurus)
            : base(tokenizer, textExtractor, thesaurus)
        {
        }

        /// <summary>
        /// Provides a delegate capable of reading all fields and associated text from an object.
        /// </summary>
        public abstract ValueTask<IEnumerable<(string field, string rawText)>> ReadAsync(TItem item, CancellationToken cancellationToken);

        /// <summary>
        /// Provides a delegate capable of reading a specific dynamic field from an object. If the field is not found on the given
        /// object, an empty enumerable will be returned and no error thrown.
        /// </summary>
        public abstract ValueTask<IEnumerable<string>> ReadAsync(TItem item, string fieldName, CancellationToken cancellationToken);
    }
}