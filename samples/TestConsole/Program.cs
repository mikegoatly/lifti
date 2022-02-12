using Lifti.Tokenization.TextExtraction;
using Lifti;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PerformanceProfiling;
using Lifti.Serialization.Binary;
using System.IO;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Lifti.Tokenization;
using System.Collections.Generic;

namespace TestConsole
{
    internal static class Program
    {
        public static async Task Main()
        {
            //do
            //{
            //    var samples = Assembly.GetExecutingAssembly().GetTypes()
            //        .Where(t => !t.IsInterface && !t.IsAbstract && typeof(ISample).IsAssignableFrom(t))
            //        .ToList();

            //    Console.Clear();
            //    Console.WriteLine("Select the sample to execute or Esc to exit:");
            //    Console.WriteLine();

            //    var firstLetter = 'a';
            //    var lastLetter = firstLetter;
            //    foreach (var sample in samples)
            //    {
            //        Console.ForegroundColor = ConsoleColor.Cyan;
            //        Console.Write($"{lastLetter}: ");
            //        Console.ResetColor();
            //        Console.WriteLine(sample.Name);
            //        lastLetter++;
            //    }

            //    Console.WriteLine();

            //    char key;
            //    do
            //    {
            //        var pressed = Console.ReadKey();
            //        if (pressed.Key == ConsoleKey.Escape)
            //        {
            //            return;
            //        }

            //        key = char.ToLowerInvariant(pressed.KeyChar);
            //        Console.CursorLeft -= 1;
            //        Console.Write(' ');
            //        Console.CursorLeft -= 1;
            //    } while (key < firstLetter|| key > lastLetter);

            //    var selectedSample = samples[key - 'a'];

            //    Console.Clear();
            //    Console.WriteLine($"Running {selectedSample.Name}");
            //    Console.WriteLine();

            //    await ((ISample)Activator.CreateInstance(selectedSample)).RunAsync();
            //} while (true);

var index = new FullTextIndexBuilder<string>()
                .WithDefaultTokenization(o => o.CaseInsensitive())
                .WithQueryParser(new CustomWildcardQueryParser())
                .Build();

            index.BeginBatchChange();
            await index.AddAsync("QueryPart", "QueryPart");
            await index.AddAsync("ExactWordQueryPart", "ExactWordQueryPart");
            await index.AddAsync("FuzzyMatchQueryPart", "FuzzyMatchQueryPart");
            await index.AddAsync("FullTextIndex", "FullTextIndex");
            await index.AddAsync("IFullTextIndex", "IFullTextIndex");
            await index.CommitBatchChangeAsync();

            //var query = new Query(
            //    new WildcardQueryPart(
            //        WildcardQueryFragment.MultiCharacter,
            //        WildcardQueryFragment.CreateText("F"),
            //        WildcardQueryFragment.MultiCharacter,
            //        WildcardQueryFragment.CreateText("T"),
            //        WildcardQueryFragment.MultiCharacter,
            //        WildcardQueryFragment.CreateText("I"),
            //        WildcardQueryFragment.MultiCharacter));

            foreach (var item in index.Search("fullti"))
            {
                Console.WriteLine(item.Key);
            }

            //await LoadCachedData(index);

            //Console.WriteLine(index.Search("*ed").Count());
        }

        private class CustomWildcardQueryParser : IQueryParser
        {
            public IQuery Parse(IIndexedFieldLookup fieldLookup, string queryText, ITokenizer tokenizer)
            {
                // Use the default tokenizer to normalize the text so it's the same as in the index
                queryText = tokenizer.Normalize(queryText);

                var queryFragments = new List<WildcardQueryFragment>();

                // Add the leading multi-character match
                queryFragments.Add(WildcardQueryFragment.MultiCharacter);

                // Add each character in the query text, with a trailing multi-character match
                foreach (var letter in queryText)
                {
                    queryFragments.Add(WildcardQueryFragment.CreateText(letter.ToString()));
                    queryFragments.Add(WildcardQueryFragment.MultiCharacter);
                }

                // Compose the final query
                return new Query(new WildcardQueryPart(queryFragments));
            }
        }

        private static async Task LoadCachedData(FullTextIndex<string> index)
        {
            var stream = File.OpenRead("serialized.dat");
            await new BinarySerializer<string>()
                .DeserializeAsync(index, stream);
        }

        private static async Task CreateWikipediaCachedData(FullTextIndex<string> index)
        {
            

            Console.WriteLine("Indexing sample wikipedia pages using an XmlTextExtractor...");

            var wikipediaTests = WikipediaDataLoader.Load(typeof(WikipediaSample));
            foreach (var (name, text) in wikipediaTests)
            {
                await index.AddAsync(name, text);
            }
            var stream = File.OpenWrite("serialized.dat");
            await new BinarySerializer<string>()
               .SerializeAsync(index, stream);

            Console.WriteLine("Saved index");
        }
    }
}
