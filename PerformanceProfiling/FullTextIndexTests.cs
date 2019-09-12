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
        private LiftiNew.Lifti.FullTextIndex<string> newIndex;
        private UpdatableFullTextIndex<string> legacyIndex;

        [GlobalSetup]
        public void Setup()
        {
            this.wikipediaData = WikipediaDataLoader.Load(typeof(Program));
            this.newIndex = CreateNewIndex();
            PopulateIndex(this.newIndex);

            this.legacyIndex = CreateLegacyIndex();
            PopulateIndex(this.legacyIndex);

        }

        [Benchmark]
        public void NewCodeIndexing()
        {
            var index = CreateNewIndex();
            PopulateIndex(index);
        }

        //[Benchmark]
        //public void NewCodeSearching()
        //{
        //    this.newIndex.Search("confiscation");
        //}

        [Benchmark]
        public void LegacyCodeIndexing()
        {
            var index = CreateLegacyIndex();
            PopulateIndex(index);
        }

        //[Benchmark]
        //public void LegacyCodeSearching()
        //{
        //    this.newIndex.Search("confiscation");
        //}

        private void PopulateIndex(UpdatableFullTextIndex<string> index)
        {
            foreach (var entry in this.wikipediaData)
            {
                index.Index(entry.name, entry.text);
            }
        }

        private static UpdatableFullTextIndex<string> CreateLegacyIndex()
        {
            var index = new UpdatableFullTextIndex<string>();
            index.WordSplitter = new XmlWordSplitter(new WordSplitter());
            index.SearchWordSplitter = new WordSplitter();
            return index;
        }

        private void PopulateIndex(LiftiNew.Lifti.FullTextIndex<string> index)
        {
            foreach (var entry in this.wikipediaData)
            {
                index.Index(entry.name, entry.text);
            }
        }

        private static LiftiNew.Lifti.FullTextIndex<string> CreateNewIndex()
        {
            return new LiftiNew.Lifti.FullTextIndex<string>(
                new LiftiNew.Lifti.FullTextIndexOptions<string>
                {
                    TokenizationOptions = { SplitOnPunctuation = true }
                },
                new LiftiNew.Lifti.Preprocessing.XmlTokenizer(
                    new LiftiNew.Lifti.InputPreprocessorPipeline(
                        new LiftiNew.Lifti.IInputPreprocessor[] {
                            new LiftiNew.Lifti.Preprocessing.CaseInsensitiveNormalizer(),
                            new LiftiNew.Lifti.Preprocessing.LatinCharacterNormalizer()
                            })),
                new LiftiNew.Lifti.IndexNodeFactory());
        }
    }
}
