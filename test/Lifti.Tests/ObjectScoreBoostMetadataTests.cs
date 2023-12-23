using FluentAssertions;
using Lifti.Tokenization.Objects;
using System;
using Xunit;

namespace Lifti.Tests
{
    public class ObjectScoreBoostMetadataTests
    {
        private const double FreshnessMultiplier = 10D;
        private const double MagnitudeMultiplier = 20D;

        private readonly ScoreBoostMetadata sut;

        public ObjectScoreBoostMetadataTests()
        {
            this.sut = new ScoreBoostMetadata(new ObjectScoreBoostOptions<object>(MagnitudeMultiplier, null, FreshnessMultiplier, null));
        }

        [Fact]
        public void FreshnessDate_WithOnlyOneValue_ReturnsMultiplier()
        {
            var metadata = DocumentMetadata(new DateTime(2022, 11, 12), null);
            this.sut.Add(metadata);

            this.sut.CalculateScoreBoost(metadata).Should().Be(FreshnessMultiplier);
        }

        [Fact]
        public void Magnitude_WithOnlyOneValue_ReturnsMultiplier()
        {
            var metadata = DocumentMetadata(null, 100433D);
            this.sut.Add(metadata);

            this.sut.CalculateScoreBoost(metadata).Should().Be(MagnitudeMultiplier);
        }

        [Fact]
        public void FreshnessDate_WithTwoValues_ReturnsFullMultiplierForMaxAndOneForMin()
        {
            var minItem = DocumentMetadata(new DateTime(1980, 11, 12), null);
            var maxItem = DocumentMetadata(new DateTime(2022, 11, 12), null);
            this.sut.Add(minItem);
            this.sut.Add(maxItem);

            this.sut.CalculateScoreBoost(minItem).Should().Be(1D);
            this.sut.CalculateScoreBoost(maxItem).Should().Be(FreshnessMultiplier);
        }

        [Fact]
        public void Magnitude_WithTwoValues_ReturnsFullMultiplierForMaxAndOneForMin()
        {
            var minItem = DocumentMetadata(null, -100D);
            var maxItem = DocumentMetadata(null, 100433D);
            this.sut.Add(minItem);
            this.sut.Add(maxItem);

            this.sut.CalculateScoreBoost(minItem).Should().Be(1D);
            this.sut.CalculateScoreBoost(maxItem).Should().Be(MagnitudeMultiplier);
        }

        [Fact]
        public void Magnitude_WithMultipleValues_CalculatesMidPoint()
        {
            var minItem = DocumentMetadata(null, -100D);
            var midItem = DocumentMetadata(null, 400D);
            var maxItem = DocumentMetadata(null, 900D);
            this.sut.Add(minItem);
            this.sut.Add(midItem);
            this.sut.Add(maxItem);

            this.sut.CalculateScoreBoost(minItem).Should().Be(1D);
            // This isn't 10 because the value ranges from 1 to MagnitudeMultiplier, not 0 to MagnitudeMultiplier.
            // That makes the mid point (19 / 2) + 1 = 10.5
            this.sut.CalculateScoreBoost(midItem).Should().Be(10.5D);
            this.sut.CalculateScoreBoost(maxItem).Should().Be(MagnitudeMultiplier);
        }

        [Fact]
        public void Item_WithNoScoreMultipliers_ReturnsOne()
        {
            var metadata = DocumentMetadata(null, null);
            this.sut.Add(metadata);

            this.sut.CalculateScoreBoost(metadata).Should().Be(1D);
        }

        private static DocumentMetadata<string> DocumentMetadata(DateTime? scoringFreshnessDate, double? scoringMagnitude)
        {
            return DocumentMetadata<string>.ForObject(1, 1, "A", new DocumentStatistics(1, 2), scoringFreshnessDate, scoringMagnitude);
        }
    }
}
