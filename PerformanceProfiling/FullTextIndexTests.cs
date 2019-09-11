extern alias LiftiNew;

using BenchmarkDotNet.Attributes;
using Lifti;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace PerformanceProfiling
{
    [ClrJob(baseline: true), CoreJob]
    [RPlotExporter, RankColumn, MemoryDiagnoser]
    public class FullTextIndexTests
    {
        private IList<(string name, string text)> wikipediaData;

        [GlobalSetup]
        public void Setup()
        {
            this.wikipediaData = WikipediaDataLoader.Load(typeof(Program));
        }

        [Benchmark]
        public void NewCodeIndexing()
        {
            var index = new LiftiNew.Lifti.FullTextIndex<string>(new LiftiNew.Lifti.FullTextIndexOptions<string>
            {
                TokenizationOptions = { SplitOnPunctuation = true }
            });

            foreach (var entry in this.wikipediaData)
            {
                index.Index(entry.name, entry.text);
            }
        }

        [Benchmark]
        public void LegacyCodeIndexing()
        {
            var index = new UpdatableFullTextIndex<string>();

            foreach (var entry in this.wikipediaData)
            {
                index.Index(entry.name, entry.text);
            }
        }
    }
}
