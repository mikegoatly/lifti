﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lifti;
using Lifti.Serialization.Binary;
using Lifti.Tokenization;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    //[RankColumn, MemoryDiagnoser]
    //[ShortRunJob(RuntimeMoniker.NetCoreApp31)]
    //[ShortRunJob(RuntimeMoniker.Net60)]
    //public class IndexSearchingBenchmarks : IndexBenchmarkBase
    //{
    //    private IFullTextIndex<string> index;

    //    [GlobalSetup]
    //    public async Task SetUp()
    //    {
    //        this.index = CreateNewIndex(4);
    //        await this.PopulateIndexAsync(this.index);
    //    }

    //    [Params("(confiscation & th*) | \"and they\"")]
    //    public string SearchCriteria { get; set; }

    //    [Benchmark]
    //    public object Searching()
    //    {
    //        return this.index.Search(this.SearchCriteria);
    //    }
    //}

    //[SimpleJob(RuntimeMoniker.NetCoreApp22)]
    //[SimpleJob(RuntimeMoniker.NetCoreApp31)]
    //[RankColumn, MemoryDiagnoser]
    //public class WordSplittingBenchmarks : IndexBenchmarkBase
    //{
    //    [Benchmark()]
    //    public void XmlWorkSplittingNew()
    //    {
    //        var splitter = new XmlTokenizer();

    //        splitter.Process(WikipediaData.SampleData[0].text).ToList();
    //    }
    //}

    //[MediumRunJob(RuntimeMoniker.NetCoreApp31)]
    [MediumRunJob(RuntimeMoniker.Net60)]
    [RankColumn, MemoryDiagnoser]
    public class SerializationBenchmarks : IndexBenchmarkBase
    {
        private BinarySerializer<string> serializer;
        private string fileName;

        [GlobalSetup]
        public async Task Setup()
        {
            var index = CreateNewIndex(2);
            await this.PopulateIndexAsync(index);

            this.serializer = new BinarySerializer<string>();
            this.fileName = $"{Guid.NewGuid()}.dat";
            using var stream = File.OpenWrite(this.fileName);
            await this.serializer.SerializeAsync(index, stream, true);
        }

        [Benchmark()]
        public async Task IndexDeserialization()
        {
            var index = CreateNewIndex(2);
            using var stream = File.OpenRead(this.fileName);
            await this.serializer.DeserializeAsync(index, stream, true);
        }
    }

    //[MediumRunJob(RuntimeMoniker.NetCoreApp31)]
    //[MediumRunJob(RuntimeMoniker.Net60)]
    //[RankColumn, MemoryDiagnoser]
    //public class FullTextIndexTests : IndexBenchmarkBase
    //{
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

        //[Benchmark()]
        //public async Task NewCodeIndexingIntraNodeTextAt4Characters()
        //{
        //    var index = CreateNewIndex(4);
        //    await this.PopulateIndexAsync(index);
        //}

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
    //}
}
