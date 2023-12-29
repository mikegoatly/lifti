using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lifti;
using System;

namespace PerformanceProfiling
{
    public class ChildNodeMapBenchmarks : IndexBenchmarkBase
    {
        private const int OperationCount = 1000000;
        private ChildNodeMap childNodeMapSingleEntry;
        private ChildNodeMap childNodeMapTwoEntries;
        private ChildNodeMap childNodeMapMultipleEntries;

        [IterationSetup]
        public void SetUp()
        {
            var testIndexNode = new IndexNode("test".AsMemory(), new ChildNodeMap(), new DocumentTokenMatchMap());
            this.childNodeMapSingleEntry = new(
                [
                    new ChildNodeMapEntry('A', testIndexNode)
                ]);

            this.childNodeMapTwoEntries = new(
                [
                    new ChildNodeMapEntry('A', testIndexNode),
                    new ChildNodeMapEntry('E', testIndexNode),
                ]);

            this.childNodeMapMultipleEntries = new(
                [
                    new ChildNodeMapEntry('F', testIndexNode),
                    new ChildNodeMapEntry('T', testIndexNode),
                    new ChildNodeMapEntry('V', testIndexNode),
                    new ChildNodeMapEntry('W', testIndexNode),
                    new ChildNodeMapEntry('X', testIndexNode),
                ]);
        }

        [Benchmark(OperationsPerInvoke = OperationCount)]
        public object SingleEntry_NotMatched()
        {
            var success = this.childNodeMapSingleEntry.TryGetValue('Z', out var nextNode);

            return nextNode;
        }

        [Benchmark(OperationsPerInvoke = OperationCount)]
        public object SingleEntry_Matched()
        {
            var success = this.childNodeMapSingleEntry.TryGetValue('A', out var nextNode);

            return nextNode;
        }

        [Benchmark(OperationsPerInvoke = OperationCount)]
        public object TwoEntries_NotMatched()
        {
            var success = this.childNodeMapTwoEntries.TryGetValue('D', out var nextNode);

            return nextNode;
        }

        [Benchmark(OperationsPerInvoke = OperationCount)]
        public object TwoEntries_Matched()
        {
            var success = this.childNodeMapTwoEntries.TryGetValue('A', out var nextNode)
                || this.childNodeMapTwoEntries.TryGetValue('E', out nextNode);

            return nextNode;
        }

        [Benchmark(OperationsPerInvoke = OperationCount)]
        public object MultipleEntries_NotMatched_BeforeStartCharacter()
        {
            var success = this.childNodeMapMultipleEntries.TryGetValue('A', out var nextNode);

            return nextNode;
        }

        [Benchmark(OperationsPerInvoke = OperationCount)]
        public object MultipleEntries_NotMatched_AfterLastCharacter()
        {
            var success = this.childNodeMapMultipleEntries.TryGetValue('Z', out var nextNode);

            return nextNode;
        }

        [Benchmark(OperationsPerInvoke = OperationCount)]
        public object MultipleEntries_NotMatched_InCharacterSet()
        {
            var success = this.childNodeMapMultipleEntries.TryGetValue('U', out var nextNode);

            return nextNode;
        }

        [Benchmark(OperationsPerInvoke = OperationCount)]
        public object MultipleEntries_Matched()
        {
            var success = this.childNodeMapMultipleEntries.TryGetValue('F', out var nextNode)
                || this.childNodeMapMultipleEntries.TryGetValue('T', out nextNode)
                || this.childNodeMapMultipleEntries.TryGetValue('V', out nextNode)
                || this.childNodeMapMultipleEntries.TryGetValue('W', out nextNode)
                || this.childNodeMapMultipleEntries.TryGetValue('X', out nextNode);

            return nextNode;
        }
    }
}
