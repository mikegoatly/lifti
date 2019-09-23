extern alias LiftiNew;
using System.Collections.Generic;

namespace PerformanceProfiling
{
    public static class WikipediaData
    {
        public static IList<(string name, string text)> SampleData { get; } = WikipediaDataLoader.Load(typeof(Program));
    }
}
