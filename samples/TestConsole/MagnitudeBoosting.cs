using Lifti;
using System.Threading.Tasks;

namespace TestConsole
{
    public class MagnitudeBoosting : SampleBase
    {
        public record Document(int Id, string Content, int Rating);

        public override async Task RunAsync()
        {
            var documents = new[]
            {
                new Document(1, "This is a document with a rating of 1", 1),
                new Document(2, "This is a document with a rating of 2", 2),
                new Document(3, "This is a document with a rating of 3", 3),
                new Document(4, "This is a document with a rating of 4", 4),
                new Document(5, "This is a document with a rating of 5", 5)
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
