using Lifti;
using Lifti.Querying;
using System;
using System.Threading.Tasks;

namespace TestConsole
{
    public static class ProgramaticallyNavigatingIndex
    {
        public static async Task RunAsync()
        {
            // Create a full text index with default settings
            var index = new FullTextIndexBuilder<string>().Build();

            // Index some sample data
            await index.AddAsync("Item1", "Catastrophe");
            await index.AddAsync("Item2", "Casualty");
            await index.AddAsync("Item3", "Cat");

            // To programatically search the index, create an index navigator instance 
            // from the index snapshot.
            using (var navigator = index.CreateNavigator())
            {
                // Navigate through the letters 'C' and 'A' (these will be the characters in their 
                // *index normalized* form)
                navigator.Process("CA".AsSpan());

                // There will be no exact matches at the current position in the index, but 3 matches 
                // when considering child matches, i.e. words starting with "ca"
                // Writes: Exact matches: 0 Exact and child matches: 3
                WriteMatchState(navigator);

                // Navigating through the 'T' of Catastrophe and Cat, but not Casualty
                navigator.Process('T');

                // Writes: Exact matches: 1 Exact and child matches: 2
                WriteMatchState(navigator);

                // Use EnumerateIndexedTokens to reverse-engineer the words that have been indexed
                // under the current location in the index, in their normalized form.
                // Writes:
                // CAT
                // CATASTROPHE
                foreach (var token in navigator.EnumerateIndexedTokens())
                {
                    Console.WriteLine(token);
                }

                // The Process method returns true if navigation was successful, and false otherwise:
                // Writes: true
                Console.WriteLine(navigator.Process('A'));

                // Writes: false
                Console.WriteLine(navigator.Process("ZOOOOM"));
            }
        }

        public static void WriteMatchState(IIndexNavigator navigator)
        {
            Console.WriteLine($@"Exact matches: {navigator.GetExactMatches().Matches.Count} 
Exact and child matches: {navigator.GetExactAndChildMatches().Matches.Count}");
        }
    }
}