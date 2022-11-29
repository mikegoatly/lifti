using Lifti.Tokenization;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tests.Querying
{
    public class FakeIndexTokenizerProvider : IIndexTokenizerProvider
    {
        private readonly Dictionary<string, IIndexTokenizer> fieldIndexTokenizers;

        public FakeIndexTokenizerProvider()
            : this(new FakeIndexTokenizer())
        {
        }

        public FakeIndexTokenizerProvider(IIndexTokenizer defaultIndexTokenizer, params (string, IIndexTokenizer)[] fieldIndexTokenizers)
        {
            this.DefaultTokenizer = defaultIndexTokenizer;
            this.fieldIndexTokenizers = fieldIndexTokenizers.ToDictionary(x => x.Item1, x => x.Item2);
        }

        public IIndexTokenizer this[string fieldName] => this.fieldIndexTokenizers[fieldName];

        public IIndexTokenizer DefaultTokenizer { get; private set; }
    }
}
