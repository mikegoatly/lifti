using FluentAssertions;
using Xunit;

namespace Lifti.Tests
{
    public class ScoreBoostValuesTests
    {
        private readonly DoubleScoreBoostValues sut;

        public ScoreBoostValuesTests()
        {
            this.sut = new DoubleScoreBoostValues();
        }

        [Fact]
        public void AddingFirstValue_ShouldInitializeMinAndMax()
        {
            this.TestAdd(100D, 100D, 100D);
        }

        [Fact]
        public void AddingMultipleValues_ShouldTrackMinAndMaxCorrectly()
        {
            this.TestAdd(50D, 50D, 50D);
            this.TestAdd(150D, 50D, 150D);
            this.TestAdd(25D, 25D, 150D);
            this.TestAdd(150D, 25D, 150D);
            this.TestAdd(175D, 25D, 175D);
        }

        [Fact]
        public void RemovingLastValueAndAddingNewValue_ShouldInitializeMinAndMax()
        {
            this.TestAdd(50D, 50D, 50D);
            this.TestAdd(100D, 50D, 100D);
            this.TestAdd(25D, 25D, 100D);

            this.TestRemove(100D, 25D, 50D);
            this.TestRemove(50D, 25D, 25D);

            // We don't care what the min and max are when we remove the last one
            this.sut.Remove(25D);

            // But the values should be reinitialized when we add a new one
            this.TestAdd(10D, 10D, 10D);
        }

        [Fact]
        public void AddingTheSameMaxValueMultipleTimes_ShouldRefCountUsage()
        {
            this.TestAdd(50D, 50D, 50D);

            // Add 100 twice
            this.TestAdd(100D, 50D, 100D);
            this.TestAdd(100D, 50D, 100D);

            // The first removal shouldn't change the min/max
            this.TestRemove(100D, 50D, 100D);

            // The second removal should
            this.TestRemove(100D, 50D, 50D);
        }

        [Fact]
        public void AddingTheSameMinValueMultipleTimes_ShouldRefCountUsage()
        {
            this.TestAdd(50D, 50D, 50D);

            // Add 25 twice
            this.TestAdd(25D, 25D, 50D);
            this.TestAdd(25D, 25D, 50D);

            // The first removal shouldn't change the min/max
            this.TestRemove(25D, 25D, 50D);

            // The second removal should
            this.TestRemove(25D, 50D, 50D);
        }

        [Fact]
        public void RemovingAnUntrackedValue_ShouldThrowException()
        {
            this.TestAdd(50D, 50D, 50D);
            this.TestAdd(100D, 50D, 100D);

            Assert.Throws<LiftiException>(() => this.sut.Remove(25D))
                .Message.Should().Be("Internal error - unexpected value removal from score boost metadata");
        }

        [Fact]
        public void ScoreBoostCalculation_ShouldReturnCorrectValue()
        {
            this.TestAdd(50D, 50D, 50D);
            this.TestAdd(100D, 50D, 100D);
            this.TestAdd(25D, 25D, 100D);

            this.sut.CalculateBoost(2D, 25D).Should().Be(1D);
            this.sut.CalculateBoost(2D, 100D).Should().Be(2D);
            this.sut.CalculateBoost(2D, 62.5).Should().Be(1.5D);
        }

        [Fact]
        public void AddingItems_AffectsScoreBoostCalculation()
        {
            this.TestAdd(50D, 50D, 50D);
            this.TestAdd(100D, 50D, 100D);

            this.sut.CalculateBoost(2D, 90D).Should().Be(1.8D);

            this.TestAdd(25D, 25D, 100D);

            this.sut.CalculateBoost(2D, 90D).Should().BeApproximately(1.866666D, 0.000001);
        }

        [Fact]
        public void RemovingItems_AffectsScoreBoostCalculation()
        {
            this.TestAdd(50D, 50D, 50D);
            this.TestAdd(100D, 50D, 100D);
            this.TestAdd(25D, 25D, 100D);

            this.sut.CalculateBoost(2D, 90D).Should().BeApproximately(1.866666D, 0.000001);

            this.TestRemove(25D, 50D, 100D);

            this.sut.CalculateBoost(2D, 90D).Should().Be(1.8D);
        }

        private void TestAdd(double value, double expectedMin, double expectedMax)
        {
            this.sut.Add(value);
            this.sut.Minimum.Should().Be(expectedMin);
            this.sut.Maximum.Should().Be(expectedMax);
        }

        private void TestRemove(double value, double expectedMin, double expectedMax)
        {
            this.sut.Remove(value);
            this.sut.Minimum.Should().Be(expectedMin);
            this.sut.Maximum.Should().Be(expectedMax);
        }
    }
}
