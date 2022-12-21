﻿using Lifti;
using Lifti.Tokenization.TextExtraction;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    public abstract class IndexBenchmarkBase
    {

        protected async Task PopulateIndexAsync(IFullTextIndex<int> index)
        {
            index.BeginBatchChange();

            await this.PopulateIndexOneByOne(index);

            await index.CommitBatchChangeAsync();
        }

        protected async Task PopulateIndexOneByOne(IFullTextIndex<int> index)
        {
            for (var i = 0; i < WikipediaData.SampleData.Count; i++)
            {
                var (name, text) = WikipediaData.SampleData[i];
                await index.AddAsync((i, name, text));
            }
        }

        protected static FullTextIndex<int> CreateNewIndex(int supportSplitAtIndex)
        {
            return new FullTextIndexBuilder<int>()
                .WithIntraNodeTextSupportedAfterIndexDepth(supportSplitAtIndex)
                .WithObjectTokenization<(int id, string name, string text)>(
                    o => o
                    .WithKey(p => p.id)
                    .WithField("Title", p => p.name)
                    .WithField("Text", p => p.text, t => t.WithStemming(), new XmlTextExtractor()))
                .Build();
        }
    }
}
