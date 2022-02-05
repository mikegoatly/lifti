using Lifti;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace TestConsole
{
    public abstract class SampleBase : ISample
    {
        public abstract Task RunAsync();

        protected static IEnumerable<SearchResult<T>> RunSearch<T>(FullTextIndex<T> index, string query, string message = null)
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

        protected static void PrintSearchResults<T>(IEnumerable<SearchResult<T>> results)
        {
            Console.WriteLine("Matched items and total score:");
            foreach (var result in results)
            {
                Console.WriteLine($"{result.Key} ({result.Score})");
            }

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
