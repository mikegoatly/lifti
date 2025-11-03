using BenchmarkDotNet.Attributes;
using Lifti;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    /// <summary>
    /// Benchmarks for IndexNavigator.Process() methods, specifically testing
    /// intra-node text traversal performance.
    /// </summary>
    [MemoryDiagnoser]
    public class IndexNavigatorBenchmarks
    {
        private IFullTextIndex<int> index = null!;

        [GlobalSetup]
        public async Task Setup()
        {
            // Create an index with words that will generate significant intra-node text
            // due to path compression. Using depth 1 for more aggressive compression.
            var builder = new FullTextIndexBuilder<int>()
                .WithIntraNodeTextSupportedAfterIndexDepth(1)
                .WithObjectTokenization<(int id, string text)>(
                    o => o
                        .WithKey(p => p.id)
                        .WithField("Text", p => p.text));

            this.index = builder.Build();

            // Index words with long common prefixes to create intra-node text
            // "inter" prefix words
            await this.index.AddAsync((1, "internationalization"));
            await this.index.AddAsync((2, "international"));
            await this.index.AddAsync((3, "internally"));
            await this.index.AddAsync((4, "internet"));
            await this.index.AddAsync((5, "interpretation"));
            await this.index.AddAsync((6, "interoperability"));

            // "comm" prefix words
            await this.index.AddAsync((7, "communication"));
            await this.index.AddAsync((8, "communities"));
            await this.index.AddAsync((9, "committee"));
            await this.index.AddAsync((10, "committing"));

            // "represent" prefix words
            await this.index.AddAsync((11, "representation"));
            await this.index.AddAsync((12, "representative"));
            await this.index.AddAsync((13, "represented"));

            // "understand" prefix words
            await this.index.AddAsync((14, "understanding"));
            await this.index.AddAsync((15, "understandable"));
            await this.index.AddAsync((16, "understands"));
        }

        /// <summary>
        /// Navigate through long intra-node text - full match
        /// Tests the optimized bulk span comparison
        /// </summary>
        [Benchmark]
        public object NavigateLongIntraNodeText_FullMatch()
        {
            using var navigator = this.index.CreateNavigator();
            navigator.Process("internationalization");
            return navigator.HasExactMatches;
        }

        /// <summary>
        /// Navigate through medium intra-node text - full match
        /// </summary>
        [Benchmark]
        public object NavigateMediumIntraNodeText_FullMatch()
        {
            using var navigator = this.index.CreateNavigator();
            navigator.Process("communication");
            return navigator.HasExactMatches;
        }

        /// <summary>
        /// Navigate through intra-node text - partial match then diverge
        /// Tests common prefix navigation (e.g., "inter" is shared)
        /// </summary>
        [Benchmark]
        public object NavigateCommonPrefix()
        {
            using var navigator = this.index.CreateNavigator();
            navigator.Process("inter");
            // Should be positioned in intra-node text with multiple possible continuations
            return navigator.EnumerateIndexedTokens();
        }

        /// <summary>
        /// Navigate through intra-node text - fail early
        /// Tests the fast-fail path when comparison fails
        /// </summary>
        [Benchmark]
        public object NavigateIntraNodeText_FailEarly()
        {
            using var navigator = this.index.CreateNavigator();
            // "intex" should fail when comparing against "inter..." intra-node text
            navigator.Process("intex");
            return navigator.HasExactMatches;
        }

        /// <summary>
        /// Multiple navigations through different intra-node texts
        /// Tests real-world scenario with multiple Process calls
        /// </summary>
        [Benchmark]
        public object MultipleNavigations()
        {
            var count = 0;

            using (var navigator = this.index.CreateNavigator())
            {
                if (navigator.Process("international"))
                    count++;
            }

            using (var navigator = this.index.CreateNavigator())
            {
                if (navigator.Process("committee"))
                    count++;
            }

            using (var navigator = this.index.CreateNavigator())
            {
                if (navigator.Process("representative"))
                    count++;
            }

            using (var navigator = this.index.CreateNavigator())
            {
                if (navigator.Process("understanding"))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Navigate character-by-character through intra-node text
        /// This tests the per-character Process(char) method for comparison
        /// </summary>
        [Benchmark]
        public object NavigateCharByChar()
        {
            using var navigator = this.index.CreateNavigator();

            // Spell out "communication" one char at a time
            navigator.Process('c');
            navigator.Process('o');
            navigator.Process('m');
            navigator.Process('m');
            navigator.Process('u');
            navigator.Process('n');
            navigator.Process('i');
            navigator.Process('c');
            navigator.Process('a');
            navigator.Process('t');
            navigator.Process('i');
            navigator.Process('o');
            navigator.Process('n');

            return navigator.HasExactMatches;
        }

        /// <summary>
        /// Navigate with ReadOnlySpan through the same word
        /// This tests the optimized Process(ReadOnlySpan<char>) method
        /// </summary>
        [Benchmark]
        public object NavigateWithSpan()
        {
            using var navigator = this.index.CreateNavigator();
            navigator.Process("communication");
            return navigator.HasExactMatches;
        }
    }
}
