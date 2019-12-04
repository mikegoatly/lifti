extern alias LiftiNew;

using BenchmarkDotNet.Attributes;
using Lifti;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    //[CoreJob]
    [RankColumn, MemoryDiagnoser]
    [ShortRunJob]
    public class IndexSearchingBenchmarks : IndexBenchmarkBase
    {
        private LiftiNew.Lifti.IFullTextIndex<string> index;
        private UpdatableFullTextIndex<string> legacyIndex;

        [GlobalSetup]
        public async Task SetUp()
        {
            this.index = CreateNewIndex(4);
            await this.PopulateIndexAsync(this.index);
            this.legacyIndex = CreateLegacyIndex();
            this.PopulateIndex(this.legacyIndex);
        }

        [Params("confiscation & and & they")]
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
            var splitter = new LiftiNew.Lifti.Tokenization.XmlTokenizer();

            splitter.Process(WikipediaData.SampleData[0].text).ToList();
        }


        [Benchmark()]
        public void XmlWordSplittingLegacy()
        {
            var splitter = new XmlWordSplitter(new WordSplitter());
            splitter.SplitWords(WikipediaData.SampleData[0].text).ToList();
        }
    }

    [MediumRunJob]
    [RankColumn, MemoryDiagnoser]
    public class FullTextIndexTests : IndexBenchmarkBase
    {
        //[Benchmark()]
        //public async Task NewCodeIndexingAlwaysSupportIntraNodeText()
        //{
        //    var index = CreateNewIndex(0);
        //    await this.PopulateIndexAsync(index);
        //}

        //[Benchmark()]
        //public async Task NewCodeIndexingAlwaysIndexCharByChar()
        //{
        //    var index = CreateNewIndex(1000);
        //    await this.PopulateIndexAsync(index);
        //}

        [Benchmark()]
        public async Task NewCodeIndexingIntraNodeTextAt4Characters()
        {
            var index = CreateNewIndex(4);
            await this.PopulateIndexAsync(index);
        }

        //[Benchmark()]
        //public async Task NewCodeIndexingOneByOneIntraNodeTextAt2Characters()
        //{
        //    var index = CreateNewIndex(2);
        //    await this.PopulateIndexOneByOneAsync(index);
        //}

        //[Benchmark()]
        //public async Task NewCodeIndexingOneByOneAlwaysSupportIntraNodeText()
        //{
        //    var index = CreateNewIndex(0);
        //    await this.PopulateIndexOneByOneAsync(index);
        //}

        //[Benchmark()]
        //public async Task NewCodeIndexingOneByOneAlwaysIndexCharByChar()
        //{
        //    var index = CreateNewIndex(1000);
        //    await this.PopulateIndexOneByOneAsync(index);
        //}

        //[Benchmark()]
        //public async Task Task NewCodeIndexingOneByOneIntraNodeTextAt4Characters()
        //{
        //    var index = CreateNewIndex(4);
        //    await this.PopulateIndexOneByOneAsync(index);
        //}

        //[Benchmark()]
        //public async Task NewCodeIndexingIntraNodeTextAt2Characters()
        //{
        //    var index = CreateNewIndex(2);
        //    await this.PopulateIndexAsync(index);
        //}

        [Benchmark()]
        public void LegacyCodeIndexing()
        {
            var index = CreateLegacyIndex();
            this.PopulateIndex(index);
        }
    }
}
