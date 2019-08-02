using System.Collections.Generic;

namespace Lifti
{
    public interface IWordSplitter : IConfiguredByOptions
    {
        IEnumerable<SplitWord> Process(string input);
    }
}