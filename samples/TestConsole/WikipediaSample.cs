using Lifti;
using Lifti.Tokenization.TextExtraction;
using PerformanceProfiling;
using System;
using System.Threading.Tasks;

namespace TestConsole
{
    public class WikipediaSample : SampleBase
    {
        public override async Task RunAsync()
        {
            var index = new FullTextIndexBuilder<string>()
                .WithTextExtractor<XmlTextExtractor>()
                .WithDefaultTokenization(o => o.WithStemming())
                .Build();

            Console.WriteLine("Indexing sample wikipedia pages using an XmlTextExtractor...");

            var wikipediaTests = WikipediaDataLoader.Load(typeof(WikipediaSample));
            foreach (var (name, text) in wikipediaTests)
            {
                await index.AddAsync(name, text);
            }

            Console.WriteLine($"Indexed {index.Count} entries");

            WaitForEnterToReturnToMenu();
        }
    }
}
