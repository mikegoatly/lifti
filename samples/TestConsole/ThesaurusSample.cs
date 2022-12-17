using static System.Reflection.Metadata.BlobBuilder;
using System.Threading.Tasks;
using System;
using Lifti;
using System.Linq;

namespace TestConsole
{
    public class ThesaurusSample : SampleBase
    {
        private record Animal(int Id, string Name, string Description);

        private static readonly Animal[] animals = new[]
{
            new Animal(1, "cat", "A domestic mammal, also known as a feline." ),
            new Animal(2, "dog", "A domesticated carnivorous mammal." ),
            new Animal(3, "rabbit", "A small, fluffy, hoofed mammal with long ears." ),
            new Animal(4, "bird", "A warm-blooded vertebrate with feathers and wings." ),
            new Animal(5, "guineapig", "A small, furry rodent with short legs and a blunt snout." ),
            new Animal(6, "rat", "A small, agile rodent with a pointed snout and long, thin tail." ),
            new Animal(7, "ferret", "A small, carnivorous mammal with a long, slender body and short legs." ),
            new Animal(8, "hamster", "A tiny, stout-bodied rodent with a short, furry tail." ),
            new Animal(9, "turtle", "A reptile with a hard, protective shell." ),
            new Animal(10, "snake", "A long, slender reptile with scales and no legs.")
        };

        public override async Task RunAsync()
        {
            Console.WriteLine("Creating an index of 10 animals using a thesaurus containing:");
            Console.WriteLine("""
                Synonyms:
                  small, tiny
                Hyponyms:
                  mammal -> cat, dog, rabbit, guineapig, rat, ferret, hamster
                  reptile -> turtle, snake
                """);

            var bookIndex = new FullTextIndexBuilder<int>() // Animals are indexed by their Id property, which is an int.
                .WithDefaultThesaurus(
                    options => options
                        .WithSynonyms("small", "tiny")
                        .WithHyponyms("mammal", "cat", "dog", "rabbit", "guineapig", "rat", "ferret", "hamster")
                        .WithHyponyms("reptile", "turtle", "snake"))
                .WithObjectTokenization<Animal>(
                    options => options
                        .WithKey(b => b.Id)
                        .WithField("Name", b => b.Name)
                        .WithField("Description", b => b.Description))
            .Build();

            await bookIndex.AddRangeAsync(animals);

            Console.WriteLine();

            await RunSearchAsync(
                bookIndex,
                "tiny",
                i => animals.First(x => x.Id == i),
                "Searching for 'tiny' will return entries that contain either 'small' or 'tiny' because they are synonyms");

            await RunSearchAsync(
                bookIndex,
                "reptile",
                i => animals.First(x => x.Id == i),
                "Searching for 'reptile' will also return entries that contain 'snake' or 'turtle' because they are hyponyms");

            await RunSearchAsync(
                bookIndex,
                "snake",
                i => animals.First(x => x.Id == i),
                "Searching for 'snake' will only return entries containing 'snake'");

            WaitForEnterToReturnToMenu();
        }
    }
}
