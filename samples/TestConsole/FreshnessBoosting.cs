using Lifti;
using System;
using System.Threading.Tasks;

namespace TestConsole
{
    public class FreshnessBoosting : SampleBase
    {
        public class Document
        {
            public int Id { get; set; }
            public string Content { get; set; }

            public DateTime UpdatedDate { get; set; }
        }

        public override async Task RunAsync()
        {
            var documents = new[]
            {
                new Document { Id = 1, Content = "This is a document that was updated 5 day ago", UpdatedDate = DateTime.UtcNow.AddDays(-5) },
                new Document { Id = 2, Content = "This is a document that was updated 4 days ago", UpdatedDate = DateTime.UtcNow.AddDays(-4) },
                new Document { Id = 3, Content = "This is a document that was updated 3 days ago", UpdatedDate = DateTime.UtcNow.AddDays(-3) },
                new Document { Id = 4, Content = "This is a document that was updated 2 days ago", UpdatedDate = DateTime.UtcNow.AddDays(-2) },
                new Document { Id = 5, Content = "This is a document that was updated 1 days ago", UpdatedDate = DateTime.UtcNow.AddDays(-1) }
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
