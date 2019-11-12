using Lifti;
using PerformanceProfiling;

namespace TestConsole
{
    public static class WikipediaSample
    {
        public static void Run()
        {
            var index = new FullTextIndexBuilder<string>()
                .WithDefaultTokenizationOptions(o => o.WithStemming().XmlContent())
                .Build();

            var wikipediaTests = WikipediaDataLoader.Load(typeof(WikipediaSample));
            foreach (var (name, text) in wikipediaTests)
            {
                index.Add(name, text);
            }
        }
    }
}
