using Lifti;
using Lifti.Tokenization.TextExtraction;
using PerformanceProfiling;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TestConsole
{
    public class WikipediaSample : SampleBase
    {
        public override async Task RunAsync()
        {
            var index = new FullTextIndexBuilder<int>()
                .WithQueryParser(o => o.AssumeFuzzySearchTerms())
                .WithObjectTokenization<(int id, string name, string text)>(
                    o => o.WithKey(x => x.id)
                        .WithField("Source", x => x.name)
                        .WithField("Content", x => x.text, textExtractor: new XmlTextExtractor()))
                .Build();

            Console.WriteLine("Indexing sample wikipedia pages using an XmlTextExtractor...");

            var wikipediaTests = WikipediaDataLoader.Load(typeof(WikipediaSample))
                .Select((x, index) => (id: index, x.name, x.text))
                .ToDictionary(x => x.id);

            await index.AddRangeAsync(wikipediaTests.Values);

            Console.WriteLine($"Indexed {index.Count} entries");
            Console.WriteLine("Type a LIFTI query, or enter to quit:");

            do
            {
                var query = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(query))
                {
                    return;
                }

                var matches = index.Search(query);

                await PrintSearchResultsAsync(matches, i => wikipediaTests[i]);
            } while (true);
        }
    }
}
