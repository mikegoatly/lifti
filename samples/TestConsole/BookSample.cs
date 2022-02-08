using Lifti;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TestConsole
{
    public class BookSample : SampleBase
    {
        private static readonly Book[] books = new[]
        {
            new Book
            {
                BookId = 1,
                Title = "The Three Body Problem",
                Authors = new[] { "Liu Cixin" },
                Synopsis = "The Three-Body Problem (Chinese: 三体; literally: 'Three-Body'; pinyin: sān tǐ) is a hard science fiction novel by the Chinese writer Liu Cixin. It is the first novel of the Remembrance of Earth's Past (Chinese: 地球往事) trilogy, but Chinese readers generally refer to the whole series by the title of this first novel.[1] The second and third novels in the trilogy are titled The Dark Forest and Death's End. The title of the first novel refers to the three-body problem in orbital mechanics."
            },
            new Book
            {
                BookId = 2,
                Title = "Dragons of Autumn Twilight",
                Authors = new[] { "Margaret Weis", "Tracy Hickman" },
                Synopsis = "Dragons of Autumn Twilight is a 1984 fantasy novel by American writers Margaret Weis and Tracy Hickman, based on a series of Dungeons & Dragons (D&D) game modules.[1] It was the first Dragonlance novel, and first in the Chronicles trilogy, which, along with the Dragonlance Legends trilogy, are generally regarded as the core novels of the Dragonlance world."
            },
        };

        public override async Task RunAsync()
        {
            var bookIndex = new FullTextIndexBuilder<int>() // Books are indexed by their BookId property, which is an int.
                .WithObjectTokenization<Book>(
                    options => options
                        .WithKey(b => b.BookId)
                        .WithField("Title", b => b.Title, tokenOptions => tokenOptions.WithStemming())
                        .WithField("Authors", b => b.Authors)
                        .WithField("Synopsis", b => b.Synopsis, tokenOptions => tokenOptions.WithStemming()))
                .Build();

            Console.WriteLine(@$"Indexing two sample books with 3 different fields, Title, Authors and Synposis:{Environment.NewLine}{string.Join(Environment.NewLine, books.Select(b => b.Title))}");
            await bookIndex.AddRangeAsync(books);
            Console.WriteLine();
            RunSearch(
                bookIndex,
                "first",
                "Both books contain 'first' in at least one field");

            RunSearch(
                bookIndex,
                "title=the",
                "Only the first book contains 'the' in the title field");
            
            WaitForEnterToReturnToMenu();
        }
    }
}
