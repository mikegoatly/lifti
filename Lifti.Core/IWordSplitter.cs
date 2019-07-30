using System.Collections.Generic;

namespace Lifti
{
    public interface IWordSplitter
    {
        IEnumerable<SplitWord> Process(string input);
    }
}