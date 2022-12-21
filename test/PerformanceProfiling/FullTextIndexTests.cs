﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lifti;
using Lifti.Serialization.Binary;
using Lifti.Tokenization;
using Lifti.Tokenization.TextExtraction;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    [MediumRunJob(RuntimeMoniker.Net481)]
    [MediumRunJob(RuntimeMoniker.Net70)]
    [MediumRunJob(RuntimeMoniker.Net60)]
    [RankColumn, MemoryDiagnoser]
    public class FullTextIndexTests : IndexBenchmarkBase
    {
        [Benchmark()]
        public async Task IndexingAlwaysSupportIntraNodeText()
        {
            var index = CreateNewIndex(0);
            await this.PopulateIndexAsync(index);
        }

        [Benchmark()]
        public async Task IndexingAlwaysIndexCharByChar()
        {
            var index = CreateNewIndex(1000);
            await this.PopulateIndexAsync(index);
        }

        [Benchmark()]
        public async Task IndexingIntraNodeTextAt4Characters()
        {
            var index = CreateNewIndex(4);
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

        //[Benchmark()]
        //public async Task Task IndexingOneByOneIntraNodeTextAt4Characters()
        //{
        //    var index = CreateNewIndex(4);
        //    await this.PopulateIndexOneByOneAsync(index);
        //}

        [Benchmark()]
        public async Task IndexingIntraNodeTextAt2Characters()
        {
            var index = CreateNewIndex(2);
            await this.PopulateIndexAsync(index);
        }
    }
}
