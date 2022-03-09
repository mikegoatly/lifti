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
            var index = new FullTextIndexBuilder<string>()
                .WithTextExtractor<XmlTextExtractor>()
                .WithQueryParser(o => o.AssumeFuzzySearchTerms())
                .Build();

            Console.WriteLine("Indexing sample wikipedia pages using an XmlTextExtractor...");

            var wikipediaTests = WikipediaDataLoader.Load(typeof(WikipediaSample));
            foreach (var (name, text) in wikipediaTests)
            {
                await index.AddAsync(name, text);
            }

            Console.WriteLine($"Indexed {index.Count} entries");
            Console.WriteLine("Type a LIFTI query, or enter to quit:");

            do
            {
                var query = Console.ReadLine();
                if (query.Length == 0)
                {
                    return;
                }

                var matches = index.Search(query).ToList();

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{matches.Count} Match(es)");

                //Console.ForegroundColor = ConsoleColor.DarkCyan;
                //foreach (var match in matches)
                //{
                //    Console.WriteLine(match.Key);
                //}

                Console.WriteLine();
                Console.ResetColor();
            } while (true);
        }
    }
}
