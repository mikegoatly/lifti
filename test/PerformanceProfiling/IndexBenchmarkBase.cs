using Lifti;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    public abstract class IndexBenchmarkBase
    {

        protected async Task PopulateIndexAsync(IFullTextIndex<string> index)
        {
            await index.AddRangeAsync(WikipediaData.SampleData);
        }

        protected async Task PopulateIndexOneByOne(IFullTextIndex<string> index)
        {
            foreach (var page in WikipediaData.SampleData)
            {
                await index.AddAsync(page);
            }
        }

        protected static IFullTextIndex<string> CreateNewIndex(int supportSplitAtIndex)
        {
            return new FullTextIndexBuilder<string>()
                .WithIntraNodeTextSupportedAfterIndexDepth(supportSplitAtIndex)
                .WithObjectTokenization<(string name, string text)>(o => o.WithKey(p => p.name).WithField("Text", p => p.text, t => t.XmlContent().WithStemming()))
                .Build();
        }
    }
}
