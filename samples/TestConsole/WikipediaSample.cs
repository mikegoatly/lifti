using Lifti;
using Lifti.Tokenization.TextExtraction;
using PerformanceProfiling;
using System.Threading.Tasks;

namespace TestConsole
{
    public static class WikipediaSample
    {
        public static async Task RunAsync()
        {
            var index = new FullTextIndexBuilder<string>()
                .WithTextExtractor<XmlTextExtractor>()
                .WithDefaultTokenization(o => o.WithStemming())
                .Build();

            var wikipediaTests = WikipediaDataLoader.Load(typeof(WikipediaSample));
            foreach (var (name, text) in wikipediaTests)
            {
                await index.AddAsync(name, text);
            }
        }
    }
}
