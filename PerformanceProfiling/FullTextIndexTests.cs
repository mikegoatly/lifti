extern alias LiftiNew;

using BenchmarkDotNet.Attributes;
using Lifti;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PerformanceProfiling
{
    //[ClrJob(baseline: true)]
    [CoreJob]
    [RPlotExporter, RankColumn, MemoryDiagnoser]
    public class FullTextIndexTests
    {
        private IList<(string name, string text)> wikipediaData;
        //private LiftiNew.Lifti.FullTextIndex<string> newIndex;
        //private UpdatableFullTextIndex<string> legacyIndex;

        [GlobalSetup]
        public void Setup()
        {
            this.wikipediaData = WikipediaDataLoader.Load(typeof(Program));
            //this.newIndex = CreateNewIndex();
            //PopulateIndex(this.newIndex);

            //this.legacyIndex = CreateLegacyIndex();
            //PopulateIndex(this.legacyIndex);

        }

        //[Benchmark()]
        //public void XmlWorkSplittingNew()
        //{
        //    var splitter = new LiftiNew.Lifti.Preprocessing.XmlTokenizer(
        //        new LiftiNew.Lifti.InputPreprocessorPipeline(Array.Empty<LiftiNew.Lifti.IInputPreprocessor>()));

        //    splitter.Process(this.wikipediaData[0].text).ToList();
        //}

        //[Benchmark()]
        //public void XmlWordSplittingLegacy()
        //{
        //    var splitter = new XmlWordSplitter(new WordSplitter());
        //    splitter.SplitWords(this.wikipediaData[0].text).ToList();
        //}

        [Benchmark()]
        public void NewCodeIndexingAlwaysSupportIntraNodeText()
        {
            var index = CreateNewIndex(-1);
            this.PopulateIndex(index);
        }

        [Benchmark()]
        public void NewCodeIndexingAlwaysIndexCharByChar()
        {
            var index = CreateNewIndex(1000);
            this.PopulateIndex(index);
        }

        [Benchmark()]
        public void NewCodeIndexingIntraNodeTextAt4Characters()
        {
            var index = CreateNewIndex(4);
            this.PopulateIndex(index);
        }

        [Benchmark()]
        public void NewCodeIndexingIntraNodeTextAt2Characters()
        {
            var index = CreateNewIndex(2);
            this.PopulateIndex(index);
        }

        //[Benchmark]
        //public void NewCodeSearching()
        //{
        //    this.newIndex.Search("confiscation");
        //}

        [Benchmark]
        public void legacycodeindexing()
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
            var index = new UpdatableFullTextIndex<string>
            {
                WordSplitter = new XmlWordSplitter(new WordSplitter()),
                SearchWordSplitter = new WordSplitter()
            };
            return index;
        }

        private void PopulateIndex(LiftiNew.Lifti.FullTextIndex<string> index)
        {
            foreach (var entry in this.wikipediaData)
            {
                index.Index(entry.name, entry.text);
            }
        }

        private static LiftiNew.Lifti.FullTextIndex<string> CreateNewIndex(int supportSplitAtIndex)
        {
            return new LiftiNew.Lifti.FullTextIndex<string>(
                new LiftiNew.Lifti.FullTextIndexOptions<string>
                {
                    TokenizationOptions = { SplitOnPunctuation = true },
                    Advanced = { SupportIntraNodeTextAfterCharacterIndex = supportSplitAtIndex }
                },
                new LiftiNew.Lifti.Preprocessing.XmlTokenizer(
                    new LiftiNew.Lifti.Preprocessing.InputPreprocessorPipeline(
                        new LiftiNew.Lifti.Preprocessing.IInputPreprocessor[] {
                            new LiftiNew.Lifti.Preprocessing.CaseInsensitiveNormalizer(),
                            new LiftiNew.Lifti.Preprocessing.LatinCharacterNormalizer()
                            })),
                new LiftiNew.Lifti.IndexNodeFactory());
        }
    }
}
