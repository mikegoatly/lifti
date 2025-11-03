using FluentAssertions;
using Lifti.Querying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class IndexNavigatorTests : QueryTestBase, IAsyncLifetime
    {
        private FullTextIndex<string> index = null!;
        private IIndexNavigator sut = null!;

        public async Task InitializeAsync()
        {
            this.index = new FullTextIndexBuilder<string>()
                .WithObjectTokenization<(string, string, string)>(
                    o => o.WithKey(i => i.Item1)
                        .WithField("Field1", i => i.Item2)
                        .WithField("Field2", i => i.Item3))
                .WithIntraNodeTextSupportedAfterIndexDepth(2)
                .Build();

            await this.index.AddAsync("A", "Triumphant elephant strode elegantly with indifference to shouting subjects, giving withering looks to individuals");
            this.sut = this.index.Snapshot.CreateNavigator();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        [Theory]
        [InlineData("IND")]
        [InlineData("INDI")]
        [InlineData("INDIF")]
        [InlineData("INDIFFERENC")]
        [InlineData("I")]
        public void GettingExactMatches_WithNoExactMatch_ShouldReturnEmptyResults(string test)
        {
            this.sut.Process(test).Should().BeTrue();
            var results = this.sut.GetExactMatches(QueryContext.Empty);
            results.Should().NotBeNull();
            results.Matches.Should().BeEmpty();
        }

        [Theory]
        [InlineData("INDIFZZ")]
        [InlineData("Z")]
        public void GettingExactMatches_WithNonMatchingTextProcessed_ShouldReturnEmptyResults(string test)
        {
            this.sut.Process(test).Should().BeFalse();
            var results = this.sut.GetExactMatches(QueryContext.Empty);
            results.Should().NotBeNull();
            results.Matches.Should().BeEmpty();
        }

        [Fact]
        public void GettingExactMatches_WithMatchingTextProcessed_ShouldReturnResults()
        {
            this.sut.Process("INDIFFERENCE").Should().BeTrue();
            var results = this.sut.GetExactMatches(QueryContext.Empty);
            results.Should().NotBeNull();
            results.Matches.Should().BeEquivalentTo(
                new[]
                {
                    ScoredToken(
                        0,
                        ScoredFieldMatch(double.Epsilon, 0, new TokenLocation(5, 42, 12)))
                },
                o => o.ComparingByMembers<ScoredToken>()
                      .ComparingByMembers<ScoredFieldMatch>()
                      .Excluding(i => i.Path.EndsWith("Score")));
        }

        [Fact]
        public async Task GettingExactMatches_WithDocumentFilter_ShouldOnlyReturnFilteredDocuments()
        {
            await this.index.AddAsync(("B", "Elephant", "Ellie"));
            await this.index.AddAsync(("C", "Elephant", "Elon"));

            this.sut = this.index.Snapshot.CreateNavigator();

            this.sut.Process("ELEPHANT").Should().BeTrue();

            var documentId = this.index.Metadata.GetMetadata("B").Id;

            var results = this.sut.GetExactMatches(new QueryContext(FilterToDocumentIds: new HashSet<int> { documentId }));

            results.Matches.Should().HaveCount(1);
            results.Matches[0].DocumentId.Should().Be(documentId);
        }

        [Fact]
        public async Task GettingExactMatches_WithFieldFilter_ShouldOnlyReturnFilteredDocuments()
        {
            await this.index.AddAsync(("B", "Elephant", "Ellie"));
            await this.index.AddAsync(("C", "Elephant", "Elephant"));

            this.sut = this.index.Snapshot.CreateNavigator();

            this.sut.Process("ELEPHANT").Should().BeTrue();

            var fieldId = this.index.FieldLookup.GetFieldInfo("Field2").Id;
            var expectedDocumentId = this.index.Metadata.GetMetadata("C").Id;

            var results = this.sut.GetExactMatches(new QueryContext(FilterToFieldId: fieldId));

            results.Matches.Should().HaveCount(1);
            results.Matches[0].DocumentId.Should().Be(expectedDocumentId);
            results.Matches[0].FieldMatches.Should().AllSatisfy(x => x.FieldId.Should().Be(fieldId));
        }

        [Fact]
        public async Task GettingExactAndChildMatches_WithDocumentFilter_ShouldOnlyReturnFilteredDocuments()
        {
            await this.index.AddAsync(("B", "Elephant", "Ellie"));
            await this.index.AddAsync(("C", "Elephant", "Elon"));

            this.sut = this.index.Snapshot.CreateNavigator();

            this.sut.Process("ELE").Should().BeTrue();

            var documentId = this.index.Metadata.GetMetadata("B").Id;

            var results = this.sut.GetExactAndChildMatches(new QueryContext(FilterToDocumentIds: new HashSet<int> { documentId }));

            results.Matches.Should().HaveCount(1);
            results.Matches[0].DocumentId.Should().Be(documentId);
        }

        [Fact]
        public async Task GettingExactAndChildMatches_WithFieldFilter_ShouldOnlyReturnFilteredDocuments()
        {
            await this.index.AddAsync(("B", "Elephant", "Ellie"));
            await this.index.AddAsync(("C", "Elephant", "Elephant"));

            this.sut = this.index.Snapshot.CreateNavigator();

            this.sut.Process("ELE").Should().BeTrue();

            var fieldId = this.index.FieldLookup.GetFieldInfo("Field2").Id;
            var expectedDocumentId = this.index.Metadata.GetMetadata("C").Id;

            var results = this.sut.GetExactAndChildMatches(new QueryContext(FilterToFieldId: fieldId));

            results.Matches.Should().HaveCount(1);
            results.Matches[0].DocumentId.Should().Be(expectedDocumentId);
            results.Matches[0].FieldMatches.Should().AllSatisfy(x => x.FieldId.Should().Be(fieldId));
        }

        [Theory]
        [InlineData("IND")]
        [InlineData("INDI")]
        [InlineData("INDIF")]
        [InlineData("INDIFFERENC")]
        [InlineData("I")]
        public void GettingExactAndChildMatches_WithNoExactMatch_ShouldReturnNonEmptyResults(string test)
        {
            this.sut.Process(test).Should().BeTrue();
            var results = this.sut.GetExactAndChildMatches(QueryContext.Empty);
            results.Should().NotBeNull();
            results.Matches.Should().NotBeEmpty();
        }

        [Fact]
        public void EnumeratingIndexedWords_WhenAtStartOfNode_ShouldReturnAppropriateWords()
        {
            this.sut.Process("INDI");
            this.sut.EnumerateIndexedTokens().Should().BeEquivalentTo(
                "INDIFFERENCE",
                "INDIVIDUALS");
        }

        [Fact]
        public void EnumeratingIndexedWords_WhenAtMidIntraNodeText_ShouldReturnAppropriateWords()
        {
            this.sut.Process("WITHERI");
            this.sut.EnumerateIndexedTokens().Should().BeEquivalentTo("WITHERING");
        }

        [Fact]
        public void EnumeratingIndexedWords_MultipleTimes_ShouldYieldSameResults()
        {
            this.sut.Process("WITHERI");
            this.sut.EnumerateIndexedTokens().ToList().Should().BeEquivalentTo(this.sut.EnumerateIndexedTokens().ToList());
        }

        [Fact]
        public void EnumeratingIndexedWords_WhenAtRoot_ShouldReturnAllWords()
        {
            this.sut.EnumerateIndexedTokens().Should().BeEquivalentTo(
                new[]
                {
                    "TRIUMPHANT",
                    "ELEPHANT",
                    "STRODE",
                    "ELEGANTLY",
                    "WITH",
                    "INDIFFERENCE",
                    "TO",
                    "SHOUTING",
                    "SUBJECTS",
                    "GIVING",
                    "WITHERING",
                    "LOOKS",
                    "INDIVIDUALS"
                },
                o => o.WithoutStrictOrdering());
        }

        [Fact]
        public void EnumeratingIndexedWords_WhenNoMatch_ShouldReturnEmptyResults()
        {
            this.sut.Process("BLABBBBHHHHHB");
            this.sut.EnumerateIndexedTokens().Should().BeEmpty();
        }

        [Fact]
        public async Task GettingExactAndChildMatches_ShouldMergeResultsAcrossFields()
        {
            await this.index.AddAsync(("B", "Zoopla Zoo Zammo", "Zany Zippy Llamas"));
            await this.index.AddAsync(("C", "Zak", "Ziggy Stardust"));

            this.sut = this.index.Snapshot.CreateNavigator();
            this.sut.Process("Z").Should().BeTrue();
            var results = this.sut.GetExactAndChildMatches(QueryContext.Empty);
            results.Should().NotBeNull();

            var expectedTokens = new[] {
                ScoredToken(
                    1,
                    [
                        ScoredFieldMatch(0D, 1, TokenLocation(0, 0, 6), TokenLocation(1, 7, 3), TokenLocation(2, 11, 5)),
                        ScoredFieldMatch(0D, 2, TokenLocation(0, 0, 4), TokenLocation(1, 5, 5))
                    ]),
                ScoredToken(
                    2,
                    [
                        ScoredFieldMatch(0D, 1, TokenLocation(0, 0, 3)),
                        ScoredFieldMatch(0D, 2, TokenLocation(0, 0, 5))
                    ])
                };

            results.Matches.Should().BeEquivalentTo(
                expectedTokens,
                o => o.ComparingByMembers<ScoredToken>()
                      .ComparingByMembers<ScoredFieldMatch>()
                      .Excluding(i => i.Path.EndsWith("Score")));
        }

        [Theory]
        [InlineData("INDIFZZ")]
        [InlineData("Z")]
        public void GettingExactAndChildMatches_WithNonMatchingTextProcessed_ShouldReturnEmptyResults(string test)
        {
            this.sut.Process(test).Should().BeFalse();
            var results = this.sut.GetExactAndChildMatches(QueryContext.Empty);
            results.Should().NotBeNull();
            results.Matches.Should().BeEmpty();
        }

        [Fact]
        public void NavigatingLetterByLetter_ShouldReturnTrueUntilNoMatch()
        {
            this.sut.Process('T').Should().BeTrue();
            this.sut.Process('R').Should().BeTrue();
            this.sut.Process('I').Should().BeTrue();
            this.sut.Process('U').Should().BeTrue();
            this.sut.Process('M').Should().BeTrue();
            this.sut.Process('P').Should().BeTrue();
            this.sut.Process('Z').Should().BeFalse();
            this.sut.Process('Z').Should().BeFalse();
        }

        [Theory]
        [InlineData("TRIUMP")]
        [InlineData("SHOUT")]
        [InlineData("WITH")]
        [InlineData("INDIVIDUALS")]
        public void NavigatingByString_ShouldReturnTrueIfEntireStringMatches(string test)
        {
            this.sut.Process(test).Should().BeTrue();
        }

        [Theory]
        [InlineData("TRIUMPZ")]
        [InlineData("SHOUTED")]
        [InlineData("WITHOUT")]
        [InlineData("ELF")]
        public void NavigatingByString_ShouldReturnFalseIfEntireStringDoesntMatch(string test)
        {
            this.sut.Process(test).Should().BeFalse();
        }

        [Fact]
        public void Bookmarking_WhenRewinding_ShouldResetToCapturedState()
        {
            this.sut.Process("INDI");

            var bookmark = this.sut.CreateBookmark();

            this.sut.Process("VIDUAL");
            this.VerifyMatchedWordIndexes(13);

            bookmark.Apply();

            this.sut.Process("F");
            this.VerifyMatchedWordIndexes(5);

            bookmark.Apply();
            this.VerifyMatchedWordIndexes(5, 13);
        }

        [Fact]
        public void Bookmarking_ShouldReuseDisposedBookmark()
        {
            this.sut.Process("INDI");

            var bookmark = this.sut.CreateBookmark();

            bookmark.Dispose();

            this.sut.Process("VIDUAL");

            var nextBookmark = this.sut.CreateBookmark();

            nextBookmark.Should().BeSameAs(bookmark);

            // And the new bookmark should be usable at the current location, not the old
            this.sut.Process("S");
            nextBookmark.Apply();
            this.VerifyMatchedWordIndexes(13);
        }

        private void VerifyMatchedWordIndexes(params int[] indexes)
        {
            var results = this.sut.GetExactAndChildMatches(QueryContext.Empty);
            results.Matches.Should().HaveCount(1);
            results.Matches[0].FieldMatches.Should().HaveCount(1);
            var fieldMatch = results.Matches[0].FieldMatches[0];
            fieldMatch.Locations.Should().HaveCount(indexes.Length);

            fieldMatch.Locations.Select(l => l.MinTokenIndex).Should().BeEquivalentTo(indexes);
        }

        [Theory]
        [InlineData("INDI", 'V', 'F')]
        [InlineData("IN", 'D')]
        [InlineData("INDV")]
        public void EnumeratingNextCharacters_ShouldReturnAllAvailableOptions(string test, params char[] expectedOptions)
        {
            this.sut.Process(test);

            this.sut.EnumerateNextCharacters().Should().BeEquivalentTo(expectedOptions);
        }

        [Fact]
        public void ProcessSpan_WhenMatchingMultipleCharactersInIntraNodeText_ShouldSucceed()
        {
            // Navigate to a position within intra-node text
            this.sut.Process("WITHER").Should().BeTrue();

            // Process multiple characters at once using the fast path
            // "ING" should match the remaining intra-node text
            this.sut.Process("ING".AsSpan()).Should().BeTrue();

            // Verify we matched the complete word
            var results = this.sut.GetExactMatches(QueryContext.Empty);
            results.Matches.Should().HaveCount(1);
        }

        [Fact]
        public void ProcessSpan_WhenPartiallyMatchingIntraNodeTextThenContinuing_ShouldNavigateCorrectly()
        {
            // Navigate to start of a word with intra-node text
            this.sut.Process("WITHER").Should().BeTrue();

            // Process span that goes beyond intra-node text - this tests the recursive call
            // "INGS" where "ING" matches intra-node text and "S" should fail to match next node
            this.sut.Process("INGS".AsSpan()).Should().BeFalse();
        }

        [Fact]
        public void ProcessSpan_WhenMismatchInMiddleOfIntraNodeText_ShouldReturnFalse()
        {
            // Navigate to position within intra-node text
            this.sut.Process("WITHER").Should().BeTrue();

            // Try to process a span that mismatches in the middle
            // Expected: "ING", Actual: "IXG"
            this.sut.Process("IXG".AsSpan()).Should().BeFalse();

            // Verify navigator is now in failed state
            this.sut.GetExactMatches(QueryContext.Empty).Matches.Should().BeEmpty();
        }

        [Fact]
        public void ProcessSpan_WhenMismatchAtStartOfIntraNodeText_ShouldReturnFalse()
        {
            // Navigate to position within intra-node text
            this.sut.Process("WITHER").Should().BeTrue();

            // Try to process a span that mismatches at the very start
            this.sut.Process("XNG".AsSpan()).Should().BeFalse();
        }

        [Fact]
        public void ProcessSpan_WhenInputExactlyMatchesRemainingIntraNodeText_ShouldStopAtNodeEnd()
        {
            // Navigate to a position within intra-node text
            this.sut.Process("WITHER").Should().BeTrue();

            // Process exactly the remaining intra-node text
            this.sut.Process("ING".AsSpan()).Should().BeTrue();

            // Should have exact match at this point
            this.sut.HasExactMatches.Should().BeTrue();
            this.sut.ExactMatchCount().Should().Be(1);
        }

        [Fact]
        public void ProcessSpan_WithBookmarkBeforeBulkProcessing_ShouldCorrectlyTrackNavigation()
        {
            // Navigate to position before intra-node text
            this.sut.Process("WITHER").Should().BeTrue();

            var bookmark = this.sut.CreateBookmark();

            // Process multiple characters using fast path
            this.sut.Process("ING".AsSpan()).Should().BeTrue();

            // Rewind and verify we can navigate the same path character by character
            bookmark.Apply();
            this.sut.Process('I').Should().BeTrue();
            this.sut.Process('N').Should().BeTrue();
            this.sut.Process('G').Should().BeTrue();

            // Should reach the same state
            this.sut.HasExactMatches.Should().BeTrue();
        }

        [Fact]
        public void ProcessSpan_ShouldCorrectlyUpdateNavigatedWithText()
        {
            // Navigate using bulk span processing
            this.sut.Process("INDI".AsSpan()).Should().BeTrue();

            // EnumerateIndexedTokens relies on navigatedWith being correctly maintained
            var tokens = this.sut.EnumerateIndexedTokens().ToList();

            // Should find both words that start with "INDI"
            tokens.Should().BeEquivalentTo("INDIFFERENCE", "INDIVIDUALS");
        }

        [Fact]
        public void ProcessSpan_WhenProcessingMixedCharAndSpan_ShouldMaintainNavigatedWith()
        {
            // Mix character-by-character and span processing
            this.sut.Process('I').Should().BeTrue();
            this.sut.Process('N').Should().BeTrue();
            this.sut.Process("DI".AsSpan()).Should().BeTrue();

            // Verify navigatedWith is correctly maintained across both methods
            var tokens = this.sut.EnumerateIndexedTokens().ToList();
            tokens.Should().BeEquivalentTo("INDIFFERENCE", "INDIVIDUALS");
        }

        [Fact]
        public void ProcessSpan_WhenBulkProcessingIntraNodeText_ShouldTrackNavigationForEnumeration()
        {
            // Navigate to a word with significant intra-node text
            this.sut.Process("WITHERI".AsSpan()).Should().BeTrue();

            // Verify that the bulk-processed text was correctly tracked
            var tokens = this.sut.EnumerateIndexedTokens().ToList();
            tokens.Should().BeEquivalentTo("WITHERING");
        }

        [Fact]
        public void ProcessSpan_WhenNavigatingThroughMultipleNodes_ShouldHandleTransitions()
        {
            // Process a longer span that transitions through intra-node text and into child nodes
            // "TRIUMPHANT" has intra-node text after depth 2
            this.sut.Process("TRIUMPHANT".AsSpan()).Should().BeTrue();

            var results = this.sut.GetExactMatches(QueryContext.Empty);
            results.Matches.Should().HaveCount(1);
        }

        [Fact]
        public void ProcessSpan_WithEmptySpan_ShouldReturnTrue()
        {
            this.sut.Process("WITH".AsSpan()).Should().BeTrue();

            // Processing empty span should succeed and not change state
            this.sut.Process(ReadOnlySpan<char>.Empty).Should().BeTrue();

            // Should still be able to continue navigating
            this.sut.Process("ERING".AsSpan()).Should().BeTrue();
            this.sut.HasExactMatches.Should().BeTrue();
        }

        [Fact]
        public void ProcessSpan_AfterFailedNavigation_ShouldRemainInFailedState()
        {
            this.sut.Process("INVALID").Should().BeFalse();

            // Any subsequent span processing should fail
            this.sut.Process("TEXT".AsSpan()).Should().BeFalse();
        }

        [Fact]
        public void ProcessSpan_WhenMatchingLongIntraNodeText_ShouldUseFastPathEfficiently()
        {
            // Navigate to a word with longer intra-node text
            this.sut.Process("I").Should().BeTrue();

            // Process the remaining characters in one go - tests bulk comparison efficiency
            this.sut.Process("NDIFFERENCE".AsSpan()).Should().BeTrue();

            this.sut.HasExactMatches.Should().BeTrue();
            this.sut.ExactMatchCount().Should().Be(1);
        }

        [Fact]
        public void ProcessSpan_WhenPartialMatchRequiresRecursion_ShouldHandleCorrectly()
        {
            // Navigate to position where remaining intra-node text is shorter than input
            this.sut.Process("WITHER").Should().BeTrue();

            // Input is longer than remaining intra-node text ("ING")
            // Should consume "ING" then recursively try to process remaining text
            // Since there's no continuation after "WITHERING", additional chars should fail
            this.sut.Process("INGLY".AsSpan()).Should().BeFalse();
        }

        [Theory]
        [InlineData("INDIFFERENCE")]
        [InlineData("INDIVIDUALS")]
        [InlineData("TRIUMPHANT")]
        [InlineData("WITHERING")]
        public void ProcessSpan_WithCompleteWords_ShouldMatchCorrectly(string word)
        {
            // Verify fast path works correctly for complete word matches
            this.sut.Process(word.AsSpan()).Should().BeTrue();
            this.sut.HasExactMatches.Should().BeTrue();
        }
    }
}
