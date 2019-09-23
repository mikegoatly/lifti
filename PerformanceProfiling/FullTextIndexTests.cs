extern alias LiftiNew;

using BenchmarkDotNet.Attributes;
using Lifti;
using System.Linq;

namespace PerformanceProfiling
{
    //[CoreJob]
    [RankColumn, MemoryDiagnoser]
    [ShortRunJob]
    public class IndexSearchingBenchmarks : IndexBenchmarkBase
    {
        private LiftiNew.Lifti.FullTextIndex<string> index;
        private UpdatableFullTextIndex<string> legacyIndex;

        [GlobalSetup]
        public void SetUp()
        {
            this.index = CreateNewIndex(4);
            this.PopulateIndex(this.index);
            this.legacyIndex = CreateLegacyIndex();
            this.PopulateIndex(this.legacyIndex);
        }

        [Params("confiscation", "justification")]
        public string SearchCriteria { get; set; }

        [Benchmark]
        public object NewCodeSearching()
        {
            return this.index.Search(this.SearchCriteria);
        }

        [Benchmark]
        public object LegacyCodeSearching()
        {
            return this.legacyIndex.Search(this.SearchCriteria);
        }
    }

    [CoreJob]
    [RankColumn, MemoryDiagnoser]
    public class WordSplittingBenchmarks : IndexBenchmarkBase
    {
        [Benchmark()]
        public void XmlWorkSplittingNew()
        {
            var splitter = new LiftiNew.Lifti.Preprocessing.XmlTokenizer();

            splitter.Process(WikipediaData.SampleData[0].text).ToList();
        }


        [Benchmark()]
        public void XmlWordSplittingLegacy()
        {
            var splitter = new XmlWordSplitter(new WordSplitter());
            splitter.SplitWords(WikipediaData.SampleData[0].text).ToList();
        }
    }

    [CoreJob]
    [RankColumn, MemoryDiagnoser]
    public class FullTextIndexTests : IndexBenchmarkBase
    {
        //[Benchmark()]
        //public void NewCodeIndexingAlwaysSupportIntraNodeText()
        //{
        //    var index = CreateNewIndex(-1);
        //    this.PopulateIndex(index);
        //}

        //[Benchmark()]
        //public void NewCodeIndexingAlwaysIndexCharByChar()
        //{
        //    var index = CreateNewIndex(1000);
        //    this.PopulateIndex(index);
        //}

        [Benchmark()]
        public void NewCodeIndexingIntraNodeTextAt4Characters()
        {
            var index = CreateNewIndex(4);
            this.PopulateIndex(index);
        }

        //[Benchmark()]
        //public void NewCodeIndexingIntraNodeTextAt2Characters()
        //{
        //    var index = CreateNewIndex(2);
        //    this.PopulateIndex(index);
        //}
    }
}
