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
            foreach (var entry in WikipediaData.SampleData)
            {
                index.Add(entry.name, entry.text, new LiftiNew.Lifti.TokenizationOptions(LiftiNew.Lifti.Tokenization.TokenizerKind.XmlContent));
            }
        }

        protected static LiftiNew.Lifti.IFullTextIndex<string> CreateNewIndex(int supportSplitAtIndex)
        {
            return new LiftiNew.Lifti.FullTextIndexBuilder<string>()
                .WithIntraNodeTextSupportedAfterIndexDepth(supportSplitAtIndex)
                .Build();
        }
    }
}
