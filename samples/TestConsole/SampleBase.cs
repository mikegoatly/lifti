using Lifti;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestConsole
{
    public abstract class SampleBase : ISample
    {
        public abstract Task RunAsync();

        protected static ISearchResults<TKey> RunSearch<TKey>(FullTextIndex<TKey> index, string query, string message = null)
        {
            if (message != null)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(message);
            }

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"Executing query: {query}");
            Console.ResetColor();

            var results = index.Search(query);
            PrintSearchResults(results);
            return results;
        }

        protected static void PrintSearchResults<TObject>(IEnumerable<SearchResult<TObject>> results)
        {
            Console.WriteLine("Matched items total score:");
            foreach (var result in results)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{result.Key} ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"({result.Score})");
            }

            Console.ResetColor();
            Console.WriteLine();
        }

        protected static async Task<ISearchResults<TKey>> RunSearchAsync<TKey, TObject>(FullTextIndex<TKey> index, string query, Func<TKey, TObject> readItem, string message = null)
        {
            if (message != null)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(message);
            }

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"Executing query: {query}");
            Console.ResetColor();

            var results = index.Search(query);
            await PrintSearchResultsAsync(results, readItem);
            return results;
        }

        protected static async Task PrintSearchResultsAsync<TKey, TObject>(ISearchResults<TKey> results, Func<TKey, TObject> readItem)
        {
            Console.WriteLine("Matched items, total score and matched phrases:");
            foreach (var result in await results.CreateMatchPhrasesAsync(readItem))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{result.SearchResult.Key} ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"({result.SearchResult.Score})");

                foreach (var fieldPhrase in result.FieldPhrases)
                {
                    Console.ResetColor();
                    Console.Write($"  {fieldPhrase.FoundIn}: ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(string.Join(", ", fieldPhrase.Phrases.Select(x => $"\"{x}\"")));
                }
            }

            Console.ResetColor();
            Console.WriteLine();
        }


        protected static void WaitForEnterToReturnToMenu()
        {
            Console.WriteLine();
            Console.WriteLine("Press enter to return to the menu");
            Console.ReadLine();
        }
    }
}
