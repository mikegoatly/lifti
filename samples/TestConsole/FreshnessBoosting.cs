using Lifti;
using System;
using System.Threading.Tasks;

namespace TestConsole
{
    public class FreshnessBoosting : SampleBase
    {
        public record Document(int Id, string Content, DateTime UpdatedDate);

        public override async Task RunAsync()
        {
            var documents = new[]
            {
                new Document(1, "This is a document that was updated 5 day ago", DateTime.UtcNow.AddDays(-5)),
                new Document(2, "This is a document that was updated 4 days ago", DateTime.UtcNow.AddDays(-4)),
                new Document(3, "This is a document that was updated 3 days ago", DateTime.UtcNow.AddDays(-3)),
                new Document(4, "This is a document that was updated 2 days ago", DateTime.UtcNow.AddDays(-2)),
                new Document(5, "This is a document that was updated 1 days ago", DateTime.UtcNow.AddDays(-1))
            };

            var index = new FullTextIndexBuilder<int>()
                .WithObjectTokenization<Document>(o => o
                    .WithKey(d => d.Id)
                    .WithField("Content", d => d.Content)
                    // Boost the score of documents that have been updated most recently, multiplying the score on a range of 1 to 2 depending
                    // the date of the document relative to the other documents.
                    .WithScoreBoosting(o => o
                        .Freshness(d => d.UpdatedDate, 2D)))
                    .Build();

            await index.AddRangeAsync(documents);

            RunSearch(
                index,
                "document",
                @"All documents contain the word 'document', but the results will be ordered by their freshness, with the most recently updated first",
                id => $"Updated on: {documents[id - 1].UpdatedDate:d}");

            WaitForEnterToReturnToMenu();
        }
    }
}
