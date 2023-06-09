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

        public abstract ValueTask<IEnumerable<(string field, string rawText)>> ReadAsync(TItem item, CancellationToken cancellationToken);
    }
}