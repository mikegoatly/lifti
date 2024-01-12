using Lifti;
using Lifti.Tokenization.TextExtraction;
using System;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    public abstract class IndexBenchmarkBase
    {

        protected async Task PopulateIndexAsync(IFullTextIndex<int> index)
        {
            index.BeginBatchChange();

            await this.PopulateIndexOneByOneAsync(index);

            await index.CommitBatchChangeAsync();
        }

        protected async Task PopulateIndexOneByOneAsync(IFullTextIndex<int> index)
        {
            for (var i = 0; i < WikipediaData.SampleData.Count; i++)
            {
                var (name, text) = WikipediaData.SampleData[i];
                await index.AddAsync((i, name, text));
            }
        }

        protected static FullTextIndex<int> CreateNewIndex(int supportSplitAtIndex, Action<FullTextIndexBuilder<int>>? additionalConfigurationActions = null)
        {
            var builder = new FullTextIndexBuilder<int>()
                .WithIntraNodeTextSupportedAfterIndexDepth(supportSplitAtIndex)
                .WithObjectTokenization<(int id, string name, string text)>(
                    o => o
                    .WithKey(p => p.id)
                    .WithField("Title", p => p.name)
                    .WithField("Text", p => p.text, t => t.WithStemming(), new XmlTextExtractor()));

            additionalConfigurationActions?.Invoke(builder);

            return builder.Build();
        }
    }
}
