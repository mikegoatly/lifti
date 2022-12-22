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
                Title = "First on Mars",
                Authors = new[] { "Cecil Warwick" },
                Synopsis = "This novel, which was first published in 1934, tells the story of a group of astronauts who become the first humans to land on the planet Mars."
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
            await RunSearchAsync(
                bookIndex,
                "first | novel",
                i => books.First(x => x.BookId == i),
                "Both books contain 'first' or 'novel' in at least one field");

            await RunSearchAsync(
                bookIndex,
                "title=the",
                i => books.First(x => x.BookId == i),
                "Only the first book contains 'the' in the title field");
            
            WaitForEnterToReturnToMenu();
        }
    }
}
