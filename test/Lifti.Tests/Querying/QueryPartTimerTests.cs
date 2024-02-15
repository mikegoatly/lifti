using FluentAssertions;
using Lifti.Querying;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class QueryPartTimerTests : QueryTestBase
    {
        [Fact]
        public void ShouldReturnSameIntermediateQueryResultsOnCompletion()
        {
            var sut = QueryPartTimer.StartNew(new ExecutionTimings(), new FakeQueryPart(1), new QueryContext());

            var results = IntermediateQueryResult();
            sut.Complete(results).Should().Be(results);
        }

        [Fact]
        public async Task ShouldRecordTimings()
        {
            var executionTimings = new ExecutionTimings();
            var queryPart = new FakeQueryPart(1);
            var sut = QueryPartTimer.StartNew(
                executionTimings,
                queryPart,
                new QueryContext());

            await Task.Delay(30);

            var results = IntermediateQueryResult(ScoredToken(1), ScoredToken(2));
            sut.Complete(results);

            executionTimings.Timings.Should().ContainSingle()
                .Which.Should().BeEquivalentTo(
                    new QueryPartExecutionDetails(
                        queryPart,
                        TimeSpan.FromMilliseconds(30),
                        2, // 2 results
                        null,
                        null),
                    // Allow for a margin of error on the time taken
                    options => options.Using<TimeSpan>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMilliseconds(20))).WhenTypeIs<TimeSpan>());
        }

        [Fact]
        public void ShouldRecordFilterCounts()
        {
            var executionTimings = new ExecutionTimings();
            var queryPart = new FakeQueryPart(1);
            var sut = QueryPartTimer.StartNew(
                executionTimings,
                queryPart,
                new QueryContext(
                    FilterToFieldId: 1,
                    FilterToDocumentIds: new HashSet<int> { 1, 2, 3 }));

            var results = IntermediateQueryResult(ScoredToken(1), ScoredToken(2));
            sut.Complete(results);

            executionTimings.Timings.Should().ContainSingle()
                .Which.Should().BeEquivalentTo(
                    new QueryPartExecutionDetails(
                        queryPart,
                        TimeSpan.Zero,
                        2,
                        3,
                        1),
                    options => options.Excluding(x => x.ExecutionTime));
        }

        [Fact]
        public void ShouldPoolAndReuseTimerInstances()
        {
            var executionTimings = new ExecutionTimings();
            var queryPart1 = new FakeQueryPart(1);
            var sut = QueryPartTimer.StartNew(executionTimings, queryPart1, new QueryContext(FilterToFieldId: 1));

            sut.Complete(IntermediateQueryResult(ScoredToken(1)));

            var queryPart2 = new FakeQueryPart(2);
            var sut2 = QueryPartTimer.StartNew(executionTimings, queryPart2, new QueryContext(FilterToDocumentIds: new HashSet<int> { 1, 2 }));

            // sut and sut2 should be the same instance
            sut.Should().BeSameAs(sut2);

            sut2.Complete(IntermediateQueryResult(ScoredToken(1), ScoredToken(2)));

            // Execution timings should contain both results
            executionTimings.Timings.Should().BeEquivalentTo(
                new[]
                {
                    new QueryPartExecutionDetails(queryPart1, TimeSpan.Zero, 1, null, 1),
                    new QueryPartExecutionDetails(queryPart2, TimeSpan.Zero, 2, 2, null)
                },
                options => options.Excluding(x => x.ExecutionTime));
        }
    }
}
