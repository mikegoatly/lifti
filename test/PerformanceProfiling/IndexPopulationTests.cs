using BenchmarkDotNet.Attributes;

using System.Threading.Tasks;

namespace PerformanceProfiling
{
    [RankColumn, MemoryDiagnoser]
    public class IndexPopulationTests : IndexBenchmarkBase
    {
        [Benchmark()]
        public async Task IndexingAlwaysSupportIntraNodeText()
        {
            var index = CreateNewIndex(0);
            await this.PopulateIndexAsync(index);
        }

        //[Benchmark()]
        //public async Task IndexingAlwaysIndexCharByChar()
        //{
        //    var index = CreateNewIndex(1000);
        //    await this.PopulateIndexAsync(index);
        //}

        [Benchmark()]
        public async Task IndexingIntraNodeTextAt4Characters()
        {
            var index = CreateNewIndex(4);
            await this.PopulateIndexAsync(index);
        }

        [Benchmark()]
        public async Task PopulatingIndexWithThesaurus()
        {
            var index = CreateNewIndex(
                4,
                b => b.WithDefaultThesaurus(t => t
                    .WithSynonyms("happy", "joyful", "cheerful")
                    .WithSynonyms("sad", "unhappy", "sorrowful")
                    .WithSynonyms("fast", "quick", "swift")
                    .WithSynonyms("slow", "lethargic", "sluggish")
                    .WithSynonyms("beautiful", "attractive", "pretty")
                    .WithSynonyms("ugly", "unattractive", "unsightly")
                    .WithSynonyms("smart", "intelligent", "clever")
                    .WithSynonyms("stupid", "foolish", "unwise")
                    .WithSynonyms("big", "large", "huge")
                    .WithSynonyms("small", "tiny", "miniature")
                    .WithSynonyms("rich", "wealthy", "affluent")
                    .WithSynonyms("poor", "impoverished", "needy")
                    .WithSynonyms("strong", "powerful", "sturdy")
                    .WithSynonyms("weak", "frail", "feeble")
                    .WithSynonyms("easy", "simple", "effortless")
                    .WithSynonyms("difficult", "hard", "challenging")
                    .WithSynonyms("cold", "chilly", "frigid")
                    .WithSynonyms("hot", "warm", "scorching")
                    .WithSynonyms("funny", "humorous", "amusing")
                    .WithSynonyms("serious", "solemn", "grave")));

            await this.PopulateIndexAsync(index);
        }

        //[Benchmark()]
        //public async Task IndexingOneByOneIntraNodeTextAt2Characters()
        //{
        //    var index = CreateNewIndex(2);
        //    await this.PopulateIndexOneByOneAsync(index);
        //}

        //[Benchmark()]
        //public async Task IndexingOneByOneAlwaysSupportIntraNodeText()
        //{
        //    var index = CreateNewIndex(0);
        //    await this.PopulateIndexOneByOneAsync(index);
        //}

        //[Benchmark()]
        //public async Task IndexingOneByOneAlwaysIndexCharByChar()
        //{
        //    var index = CreateNewIndex(1000);
        //    await this.PopulateIndexOneByOneAsync(index);
        //}

        [Benchmark()]
        public async Task IndexingOneByOneIntraNodeTextAt4Characters()
        {
            var index = CreateNewIndex(4);
            await this.PopulateIndexOneByOneAsync(index);
        }

        //[Benchmark()]
        //public async Task IndexingIntraNodeTextAt2Characters()
        //{
        //    var index = CreateNewIndex(2);
        //    await this.PopulateIndexAsync(index);
        //}
    }
}
