using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Lifti.Tests.Fakes;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class AnchoredWordQueryPartTests : QueryTestBase
    {
        [Fact]
        public void Constructor_WhenBothAnchorsAreFalse_ShouldThrowException()
        {
            var action = () => new AnchoredWordQueryPart("test", requireStart: false, requireEnd: false);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*" + ExceptionMessages.MustAnchorAtStartEndOrBoth + "*");
        }

        [Fact]
        public void EvaluatingNavigationInIndex_WithStartAnchor_ShouldOnlyReturnMatchesAtStart()
        {
            var sut = new AnchoredWordQueryPart("test", requireStart: true, requireEnd: false);

            // Create matches with specific token locations
            var matches = new[]
            {
                ScoredToken(1, ScoredFieldMatch(1D, 1, 0)),     // Doc 1, Field 1, token at index 0 (START - MATCH)
                ScoredToken(2, ScoredFieldMatch(1D, 1, 3)),     // Doc 2, Field 1, token at index 3 (NOT MATCH)
                ScoredToken(3, ScoredFieldMatch(1D, 1, 0))      // Doc 3, Field 1, token at index 0 (START - MATCH)
            };

            var navigator = FakeIndexNavigator.ReturningExactMatches(matches);

            // Set up metadata with field statistics
            var metadata = new FakeIndexMetadata<int>(
                3,
                documentMetadata:
                [
                    (1, DocumentMetadata.ForLooseText(1, 1, new(new Dictionary<byte, FieldStatistics> { { 1, new(10, 9) } }, 10))),
                    (2, DocumentMetadata.ForLooseText(2, 2, new(new Dictionary<byte, FieldStatistics> { { 1, new(5, 4) } }, 5))),
                    (3, DocumentMetadata.ForLooseText(3, 3, new(new Dictionary<byte, FieldStatistics> { { 1, new(8, 7) } }, 8)))
                ]);

            navigator.Snapshot = new FakeIndexSnapshot(metadata);

            var result = sut.Evaluate(() => navigator, QueryContext.Empty);

            // Only doc 1 and doc 3 should match (token at index 0)
            result.Matches.Should().HaveCount(2);
            result.Matches.Select(m => m.DocumentId).Should().BeEquivalentTo(new[] { 1, 3 });
            result.Matches.SelectMany(m => m.FieldMatches).All(fm => fm.FieldId == 1).Should().BeTrue();
        }

        [Fact]
        public void EvaluatingNavigationInIndex_WithEndAnchor_ShouldOnlyReturnMatchesAtEnd()
        {
            var sut = new AnchoredWordQueryPart("test", requireStart: false, requireEnd: true);

            var matches = new[]
            {
                ScoredToken(1, ScoredFieldMatch(1D, 1, 9)),     // Doc 1, token at index 9 (END - MATCH)
                ScoredToken(2, ScoredFieldMatch(1D, 1, 2)),     // Doc 2, token at index 2 (NOT MATCH)
                ScoredToken(3, ScoredFieldMatch(1D, 1, 7))      // Doc 3, token at index 7 (END - MATCH)
            };

            var navigator = FakeIndexNavigator.ReturningExactMatches(matches);
            var metadata = new FakeIndexMetadata<int>(
                3,
                documentMetadata:
                [
                    (1, DocumentMetadata.ForLooseText(1, 1, new(new Dictionary<byte, FieldStatistics> { { 1, new(10, 9) } }, 10))),
                    (2, DocumentMetadata.ForLooseText(2, 2, new(new Dictionary<byte, FieldStatistics> { { 1, new(5, 4) } }, 5))),
                    (3, DocumentMetadata.ForLooseText(3, 3, new(new Dictionary<byte, FieldStatistics> { { 1, new(8, 7) } }, 8)))
                ]);

            navigator.Snapshot = new FakeIndexSnapshot(metadata);

            var result = sut.Evaluate(() => navigator, QueryContext.Empty);

            // Only doc 1 and doc 3 should match (token at last index)
            result.Matches.Should().HaveCount(2);
            result.Matches.Select(m => m.DocumentId).Should().BeEquivalentTo(new[] { 1, 3 });
        }

        [Fact]
        public void EvaluatingNavigationInIndex_WithBothAnchors_ShouldOnlyReturnExactMatches()
        {
            var sut = new AnchoredWordQueryPart("test", requireStart: true, requireEnd: true);

            var matches = new[]
            {
                ScoredToken(1, ScoredFieldMatch(1D, 1, 0)),     // Doc 1, single token field (MATCH)
                ScoredToken(2, ScoredFieldMatch(1D, 1, 0)),     // Doc 2, token at 0 but field has 5 tokens (NOT MATCH)
                ScoredToken(3, ScoredFieldMatch(1D, 1, 0))      // Doc 3, single token field (MATCH)
            };

            var navigator = FakeIndexNavigator.ReturningExactMatches(matches);
            var metadata = new FakeIndexMetadata<int>(
                3,
                documentMetadata:
                [
                    (1, DocumentMetadata.ForLooseText(1, 1, new(new Dictionary<byte, FieldStatistics> { { 1, new(1, 0) } }, 1))),
                    (2, DocumentMetadata.ForLooseText(2, 2, new(new Dictionary<byte, FieldStatistics> { { 1, new(5, 4) } }, 5))),
                    (3, DocumentMetadata.ForLooseText(3, 3, new(new Dictionary<byte, FieldStatistics> { { 1, new(1, 0) } }, 1)))
                ]);

            navigator.Snapshot = new FakeIndexSnapshot(metadata);

            var result = sut.Evaluate(() => navigator, QueryContext.Empty);

            // Only doc 1 and doc 3 should match (single token fields where start == end)
            result.Matches.Should().HaveCount(2);
            result.Matches.Select(m => m.DocumentId).Should().BeEquivalentTo(new[] { 1, 3 });
        }

        [Fact]
        public void EvaluatingNavigationInIndex_WithCompositeLocations_ShouldFilterByMinAndMaxTokenIndex()
        {
            var sut = new AnchoredWordQueryPart("test", requireStart: true, requireEnd: true);

            var matches = new[]
            {
                ScoredToken(1, ScoredFieldMatch(1D, 1, CompositeTokenLocation(0, 4))),  // Spans 0-4 in 6-token field (0-5), NOT MATCH
                ScoredToken(2, ScoredFieldMatch(1D, 1, CompositeTokenLocation(0)))      // Single location at 0 in 1-token field (MATCH)
            };

            var navigator = FakeIndexNavigator.ReturningExactMatches(matches);
            var metadata = new FakeIndexMetadata<int>(
                2,
                documentMetadata:
                [
                    (1, DocumentMetadata.ForLooseText(1, 1, new(new Dictionary<byte, FieldStatistics> { { 1, new(6, 5) } }, 6))),
                    (2, DocumentMetadata.ForLooseText(2, 2, new(new Dictionary<byte, FieldStatistics> { { 1, new(1, 0) } }, 1)))
                ]);

            navigator.Snapshot = new FakeIndexSnapshot(metadata);

            var result = sut.Evaluate(() => navigator, QueryContext.Empty);

            // Only doc 2 should match (single token covers entire field)
            result.Matches.Should().HaveCount(1);
            result.Matches[0].DocumentId.Should().Be(2);
        }

        [Fact]
        public void EvaluatingNavigationInIndex_WithMultipleFields_ShouldFilterEachFieldIndependently()
        {
            var sut = new AnchoredWordQueryPart("test", requireStart: true, requireEnd: false);

            // Doc 1 has two field matches
            var matches = new[]
            {
                ScoredToken(1,
                    ScoredFieldMatch(1D, 1, 0),      // Field 1: token at 0 (START - MATCH)
                    ScoredFieldMatch(1D, 2, 3))      // Field 2: token at 3 (NOT MATCH)
            };

            var navigator = FakeIndexNavigator.ReturningExactMatches(matches);
            var metadata = new FakeIndexMetadata<int>(
                1,
                documentMetadata:
                [
                    (1, DocumentMetadata.ForLooseText(1, 1, new(
                        new Dictionary<byte, FieldStatistics>
                        {
                            { 1, new(10, 9) },
                            { 2, new(5, 4) }
                        }, 15)))
                ]);

            navigator.Snapshot = new FakeIndexSnapshot(metadata);

            var result = sut.Evaluate(() => navigator, QueryContext.Empty);

            // Should return doc 1 with only field 1
            result.Matches.Should().HaveCount(1);
            result.Matches[0].FieldMatches.Should().HaveCount(1);
            result.Matches[0].FieldMatches[0].FieldId.Should().Be(1);
        }

        [Fact]
        public void EvaluatingNavigationInIndex_WhenNoMatchesPassFilter_ShouldReturnEmptyResults()
        {
            var sut = new AnchoredWordQueryPart("test", requireStart: true, requireEnd: false);

            var matches = new[]
            {
                ScoredToken(1, ScoredFieldMatch(1D, 1, 5)),     // Middle of field
                ScoredToken(2, ScoredFieldMatch(1D, 1, 2))      // Middle of field
            };

            var navigator = FakeIndexNavigator.ReturningExactMatches(matches);
            var metadata = new FakeIndexMetadata<int>(
                2,
                documentMetadata:
                [
                    (1, DocumentMetadata.ForLooseText(1, 1, new(new Dictionary<byte, FieldStatistics> { { 1, new(10, 9) } }, 10))),
                    (2, DocumentMetadata.ForLooseText(2, 2, new(new Dictionary<byte, FieldStatistics> { { 1, new(5, 4) } }, 5)))
                ]);

            navigator.Snapshot = new FakeIndexSnapshot(metadata);

            var result = sut.Evaluate(() => navigator, QueryContext.Empty);

            result.Matches.Should().BeEmpty();
        }

        [Fact]
        public void EvaluatingNavigationInIndex_WhenFieldStatisticsAreMissing_ShouldSkipField()
        {
            var sut = new AnchoredWordQueryPart("test", requireStart: true, requireEnd: false);

            var matches = new[]
            {
                ScoredToken(1, ScoredFieldMatch(1D, 1, 0))      // Token at start
            };

            var navigator = FakeIndexNavigator.ReturningExactMatches(matches);

            // Create metadata without field statistics for field 1
            var metadata = new FakeIndexMetadata<int>(
                1,
                new IndexStatistics(),
                [
                    (1, DocumentMetadata.ForLooseText(
                        1,
                        1,
                        new(new Dictionary<byte, FieldStatistics>(), 0)))
                ]);

            navigator.Snapshot = new FakeIndexSnapshot(metadata);

            var result = sut.Evaluate(() => navigator, QueryContext.Empty);

            // Field statistics missing, so the field match is skipped
            result.Matches.Should().BeEmpty();
        }

        [Fact]
        public void EvaluatingNavigationInIndex_WhenLastTokenIndexIsNegative_ShouldThrowException()
        {
            var sut = new AnchoredWordQueryPart("test", requireStart: true, requireEnd: false);

            var matches = new[]
            {
                ScoredToken(1, ScoredFieldMatch(1D, 1, 0))
            };

            var navigator = FakeIndexNavigator.ReturningExactMatches(matches);
            var metadata = new FakeIndexMetadata<int>(
                1,
                documentMetadata:
                [
                    (1, DocumentMetadata.ForLooseText(1, 1, new(new Dictionary<byte, FieldStatistics> { { 1, new(10, -1) } }, 10)))
                ]);

            navigator.Snapshot = new FakeIndexSnapshot(metadata);

            var action = () => sut.Evaluate(() => navigator, QueryContext.Empty);

            action.Should().Throw<LiftiException>()
                .WithMessage(ExceptionMessages.MissingLastTokenIndexMetadata);
        }

        [Fact]
        public void ToString_WithStartAnchorOnly_ShouldShowStartMarker()
        {
            var sut = new AnchoredWordQueryPart("word", requireStart: true, requireEnd: false);

            sut.ToString().Should().Be("<<word");
        }

        [Fact]
        public void ToString_WithEndAnchorOnly_ShouldShowEndMarker()
        {
            var sut = new AnchoredWordQueryPart("word", requireStart: false, requireEnd: true);

            sut.ToString().Should().Be("word>>");
        }

        [Fact]
        public void ToString_WithBothAnchors_ShouldShowBothMarkers()
        {
            var sut = new AnchoredWordQueryPart("word", requireStart: true, requireEnd: true);

            sut.ToString().Should().Be("<<word>>");
        }

        [Fact]
        public void ToString_WithScoreBoost_ShouldIncludeBoost()
        {
            var sut = new AnchoredWordQueryPart("word", requireStart: true, requireEnd: false, scoreBoost: 2.5);

            sut.ToString().Should().Be("<<word^2.5");
        }

        [Fact]
        public void RequireStart_Property_ShouldReturnConstructorValue()
        {
            var sut = new AnchoredWordQueryPart("test", requireStart: true, requireEnd: false);

            sut.RequireStart.Should().BeTrue();
        }

        [Fact]
        public void RequireEnd_Property_ShouldReturnConstructorValue()
        {
            var sut = new AnchoredWordQueryPart("test", requireStart: false, requireEnd: true);

            sut.RequireEnd.Should().BeTrue();
        }

        [Fact]
        public void CalculateWeighting_ShouldUseBaseImplementation()
        {
            var sut = new AnchoredWordQueryPart("test", requireStart: true, requireEnd: false);
            var navigator = FakeIndexNavigator.ReturningExactMatches(1, 2);
            navigator.Snapshot = new FakeIndexSnapshot(new FakeIndexMetadata<int>(10));

            // Should calculate weighting based on matched documents vs total documents
            // 2 matches out of 10 documents = 0.2
            var result = sut.CalculateWeighting(() => navigator);

            result.Should().Be(0.2D);
        }
    }
}
