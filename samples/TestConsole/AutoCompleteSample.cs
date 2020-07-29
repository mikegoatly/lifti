using Lifti;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole
{
    public class AutoCompleteHelper
    {
        private FullTextIndex<int> index;

        public async Task InitializeAsync()
        {
            this.index = await CreateIndexAsync();
        }

        public IEnumerable<string> GetSuggestions(string input)
        {
            using var navigator = index.CreateNavigator();
            navigator.Process(input.AsSpan());
            return navigator.EnumerateIndexedTokens().ToList();
        }

        private static async Task<FullTextIndex<int>> CreateIndexAsync()
        {
            var index = new FullTextIndexBuilder<int>()
                .Build();

            index.BeginBatchChange();

            var colorProperties = typeof(Color).GetProperties(BindingFlags.Static | BindingFlags.Public);
            var i = 0;
            foreach (var prop in colorProperties)
            {
                await index.AddAsync(i++, prop.Name);
            }

            await index.CommitBatchChangeAsync();

            return index;
        }
    }

    /// <summary>
    /// This example shows how you can use an index as an auto-complete solution.
    /// </summary>
    public static class AutoCompleteSample
    {
        public static async Task RunAsync()
        {
            Console.Clear();

            var autoCompleteHelper = new AutoCompleteHelper();
            await autoCompleteHelper.InitializeAsync();

            var input = new StringBuilder();
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("Type the name of a color (Ctrl-C to quit): ");

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(input.ToString());

                if (input.Length > 0)
                {
                    var matchingColors = autoCompleteHelper.GetSuggestions(input.ToString());

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("Matches: ");
                    foreach (var match in matchingColors)
                    {
                        Console.Write(match);
                        Console.Write(" ");
                    }
                }

                var next = Console.ReadKey();
                if (next.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input.Length -= 1;
                }
                else
                {
                    input.Append(char.ToUpperInvariant(next.KeyChar));
                }

                Console.Clear();
            }
        }


    }
}
