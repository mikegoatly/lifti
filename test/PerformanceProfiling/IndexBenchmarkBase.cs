extern alias LiftiNew;
using Lifti;
using Lifti.Querying;

namespace PerformanceProfiling
{
    public abstract class IndexBenchmarkBase
    {
        protected void PopulateIndex(UpdatableFullTextIndex<string> index)
        {
            foreach (var entry in WikipediaData.SampleData)
            {
                index.Index(entry.name, entry.text);
            }
        }

        protected static UpdatableFullTextIndex<string> CreateLegacyIndex()
        {
            var index = new UpdatableFullTextIndex<string>
            {
                WordSplitter = new XmlWordSplitter(new StemmingWordSplitter()),
                SearchWordSplitter = new WordSplitter(),
                QueryParser = new LiftiQueryParser()
            };
            return index;
        }
   
        protected void PopulateIndex(LiftiNew.Lifti.IFullTextIndex<string> index)
        {
            index.AddRange(WikipediaData.SampleData);
        }

        protected void PopulateIndexOneByOne(LiftiNew.Lifti.IFullTextIndex<string> index)
        {
            foreach (var page in WikipediaData.SampleData)
            {
                index.Add(page);
            }
        }

        protected static LiftiNew.Lifti.IFullTextIndex<string> CreateNewIndex(int supportSplitAtIndex)
        {
            return new LiftiNew.Lifti.FullTextIndexBuilder<string>()
                .WithIntraNodeTextSupportedAfterIndexDepth(supportSplitAtIndex)
                .WithItemTokenization<(string name, string text)>(o => o.WithKey(p => p.name).WithField("Text", p => p.text, t => t.XmlContent().WithStemming()))
                .Build();
        }
    }
}
