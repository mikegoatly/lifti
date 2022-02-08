using Lifti;
using Lifti.Querying;
using System;
using System.Threading.Tasks;

namespace TestConsole
{
    public class ProgramaticallyNavigatingIndex : SampleBase
    {
        public override async Task RunAsync()
        {
            // Create a full text index with default settings
            var index = new FullTextIndexBuilder<string>().Build();

            // Index some sample data
            Console.WriteLine("Adding Item1: 'Catastrophe', Item2: 'Casualty' and Item3: 'Cat' to the index");
            await index.AddAsync("Item1", "Catastrophe");
            await index.AddAsync("Item2", "Casualty");
            await index.AddAsync("Item3", "Cat");

            // To programatically search the index, create an index navigator instance 
            // from the index snapshot.
            using (var navigator = index.CreateNavigator())
            {
                // Navigate through the letters 'C' and 'A' (these will be the characters in their 
                // *index normalized* form)
                Console.WriteLine("Navigating the characters 'CA'");
                Console.WriteLine(navigator.Process("CA".AsSpan()));

                // There will be no exact matches at the current position in the index, but 3 matches 
                // when considering child matches, i.e. words starting with "ca"
                // Writes: Exact matches: 0 Exact and child matches: 3
                WriteMatchState(navigator);

                // Navigating through the 'T' of Catastrophe and Cat, but not Casualty
                Console.WriteLine("Navigating the character 'T'");
                Console.WriteLine(navigator.Process('T'));

                // Writes: Exact matches: 1 Exact and child matches: 2
                WriteMatchState(navigator);

                // Use EnumerateIndexedTokens to reverse-engineer the words that have been indexed
                // under the current location in the index, in their normalized form.
                // Writes:
                // CAT
                // CATASTROPHE
                Console.WriteLine("Enumerating the indexed tokens at this location:");
                foreach (var token in navigator.EnumerateIndexedTokens())
                {
                    Console.WriteLine(token);
                }

                // The Process method returns true if navigation was successful, and false otherwise:
                // Writes: true
                Console.WriteLine("Navigating the character 'A'");
                Console.WriteLine(navigator.Process('A'));
                Console.WriteLine();

                // Creating a bookmark allows you to attempt navigation but return to the current point later on
                Console.WriteLine("Creating a bookmark");
                var bookmark = navigator.CreateBookmark();

                // Writes: false
                Console.WriteLine("Navigating the characters 'ZOOOOM'");
                Console.WriteLine(navigator.Process("ZOOOOM"));
                Console.WriteLine();

                Console.WriteLine("Resetting the navigator to the bookmarked location");
                bookmark.Apply();
                Console.WriteLine();

                Console.WriteLine("Navigating the characters 'STROPHE'");
                Console.WriteLine(navigator.Process("STROPHE".AsSpan()));

                WaitForEnterToReturnToMenu();
            }
        }

        public static void WriteMatchState(IIndexNavigator navigator)
        {
            Console.WriteLine($@"
At this location in the index:
Exact matches: {navigator.GetExactMatches().Matches.Count} 
Exact and child matches: {navigator.GetExactAndChildMatches().Matches.Count}");

            Console.WriteLine();
        }
    }
}