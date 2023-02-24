using Lifti;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole
{
    public class IndexingNestedObjectsSample : SampleBase
    {
        private static readonly Book[] books = new[]
        {
            new Book
            {
                BookId = 1,
                Extracts = new[] 
                {
                    new BookExtract("The Three-Body Problem (Chinese: 三体; literally: 'Three-Body'; pinyin: sān tǐ) is a hard science fiction novel by the Chinese writer Liu Cixin."),
                    new BookExtract("It is the first novel of the Remembrance of Earth's Past (Chinese: 地球往事) trilogy, but Chinese readers generally refer to the whole series by the title of this first novel.[1]"),
                    new BookExtract("The second and third novels in the trilogy are titled The Dark Forest and Death's End. The title of the first novel refers to the three-body problem in orbital mechanics.")
                }
            },
            new Book
            {
                BookId = 2,
                Extracts = new[] { new BookExtract("This novel, which was first published in 1934, tells the story of a group of astronauts who become the first humans to land on the planet Mars.") }
            },
        };

        public override async Task RunAsync()
        {
            var index = new FullTextIndexBuilder<int>()
                .WithObjectTokenization<Book>(
                    o => o.WithKey(x => x.BookId)
                        .WithField("Extract", x => x.Extracts, extract => extract.Text))
                .Build();

            await index.AddRangeAsync(books);

            RunSearch(index, "fiction", "'fiction' appears in the first extract of book 1");
            RunSearch(index, "Remembrance", "'Remembrance' appears in the second extract of book 1");
            RunSearch(index, "mechanics", "'mechanics' appears in the third extract of book 1");

            WaitForEnterToReturnToMenu();
        }
    }
}
