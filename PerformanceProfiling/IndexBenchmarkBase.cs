extern alias LiftiNew;
using Lifti;

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
                WordSplitter = new XmlWordSplitter(new WordSplitter()),
                SearchWordSplitter = new WordSplitter()
            };
            return index;
        }
   
        protected void PopulateIndex(LiftiNew.Lifti.FullTextIndex<string> index)
        {
            foreach (var entry in WikipediaData.SampleData)
            {
                index.Index(entry.name, entry.text, new LiftiNew.Lifti.TokenizationOptions(LiftiNew.Lifti.Tokenization.TokenizerKind.XmlContent));
            }
        }

        protected static LiftiNew.Lifti.FullTextIndex<string> CreateNewIndex(int supportSplitAtIndex)
        {
            return new LiftiNew.Lifti.FullTextIndex<string>(
                new LiftiNew.Lifti.FullTextIndexConfiguration<string>
                {
                    Advanced = { SupportIntraNodeTextAfterCharacterIndex = supportSplitAtIndex }
                });
        }
    }
}
