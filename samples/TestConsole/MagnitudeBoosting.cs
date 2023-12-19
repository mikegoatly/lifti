using Lifti;
using System.Threading.Tasks;

namespace TestConsole
{
    public class MagnitudeBoosting : SampleBase
    {
        public class Document
        {
            public int Id { get; set; }
            public string Content { get; set; }
            public int Rating { get; set; }
        }

        public override async Task RunAsync()
        {
            var documents = new[]
            {
                new Document { Id = 1, Content = "This is a document with a rating of 1", Rating = 1 },
                new Document { Id = 2, Content = "This is a document with a rating of 2", Rating = 2 },
                new Document { Id = 3, Content = "This is a document with a rating of 3", Rating = 3 },
                new Document { Id = 4, Content = "This is a document with a rating of 4", Rating = 4 },
                new Document { Id = 5, Content = "This is a document with a rating of 5", Rating = 5 }
            };

            var index = new FullTextIndexBuilder<int>()
                .WithObjectTokenization<Document>(o => o
                    .WithKey(d => d.Id)
                    .WithField("Content", d => d.Content)
                    // Boost the score of documents with a higher rating multiplying the score on a range of 1 to 2 depending
                    // on the rating.
                    .WithScoreBoosting(o => o.Magnitude(d => d.Rating, 2D)))
                .Build();

            await index.AddRangeAsync(documents);

            RunSearch(
                index,
                "document",
                @"All documents contain the word 'document', but the results will be ordered by their rating, with the highest rating first",
                id => $"Star rating: {documents[id - 1].Rating}");

            WaitForEnterToReturnToMenu();
        }
    }
}
