using FluentAssertions;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Lifti.Tests
{
    public class IdPoolTests
    {
        private static readonly DocumentStatistics item1DocumentStatistics = DocumentStatistics((1, 100));
        private static readonly DocumentStatistics item2DocumentStatistics = DocumentStatistics((1, 50), (2, 200));

        private readonly IdPool<string> sut;
        private readonly int id1;
        private readonly int id2;

        public IdPoolTests()
        {
            this.sut = new IdPool<string>();
            this.id1 = this.sut.Add("1", item1DocumentStatistics);
            this.id2 = this.sut.Add("2", item2DocumentStatistics);
        }

        [Fact]
        public void Add_ItemOnly_ShouldIncrementIndexStatistics()
        {
            this.sut.IndexStatistics.Should().BeEquivalentTo(IndexStatistics((1, 150), (2, 200)));
        }

        [Fact]
        public void Add_ItemOnly_ShouldIncrementWhenNoItemsReturned()
        {
            this.id1.Should().BeLessThan(this.id2);
        }

        [Fact]
        public void Add_ItemOnly_ShouldThrowExceptionIfItemAlreadyIndexed()
        {
            Assert.Throws<LiftiException>(() => this.sut.Add("1", DocumentStatistics()))
                .Message.Should().Be("Item already indexed");
        }

        [Fact]
        public void Add_ItemWithId_ShouldThrowExceptionIfItemAlreadyIndexed()
        {
            Assert.Throws<LiftiException>(() => this.sut.Add(9, "1", DocumentStatistics()))
                .Message.Should().Be("Item already indexed");
        }

        [Fact]
        public void Add_ItemWithId_ShouldThrowExceptionIfIdAlreadyUsedAlreadyIndexed()
        {
            Assert.Throws<LiftiException>(() => this.sut.Add(1, "9", DocumentStatistics()))
                .Message.Should().Be("Id 1 is already registered in the index.");
        }

        [Fact]
        public void Add_ItemWithId_ShouldAddItemToIndex()
        {
            var documentStatistics = DocumentStatistics((1, 20), (2, 50), (3, 10));
            this.sut.Add(9, "9", documentStatistics);
            this.sut.GetMetadata(9).Should().BeEquivalentTo(
                new ItemMetadata<string>(
                    9,
                    "9",
                    documentStatistics));
        }

        [Fact]
        public void Add_ItemWithId_ShouldAdjustIndexStatistics()
        {
            var documentStatistics = DocumentStatistics((1, 20), (2, 50), (3, 10));
            this.sut.Add(9, "9", documentStatistics);
            this.sut.IndexStatistics.Should().BeEquivalentTo(
                IndexStatistics((1, 170), (2, 250), (3, 10)));
        }

        [Fact]
        public void Add_ItemWithId_ShouldResetTheNextIdBasedOnTheHighestIndexedId()
        {
            this.sut.Add(10, "10", DocumentStatistics((10, 10)));
            this.sut.Add(9, "9", DocumentStatistics((9, 9)));

            this.sut.Add("7", DocumentStatistics((7, 7)));

            this.sut.GetIndexedItems().Should().BeEquivalentTo(
                new ItemMetadata<string>(0, "1", item1DocumentStatistics),
                new ItemMetadata<string>(1, "2", item2DocumentStatistics),
                new ItemMetadata<string>(9, "9", DocumentStatistics((9, 9))),
                new ItemMetadata<string>(10, "10", DocumentStatistics((10, 10))),
                new ItemMetadata<string>(11, "7", DocumentStatistics((7, 7)))
            );
        }

        [Fact]
        public void Count_ShouldReturnCorrectValue()
        {
            this.sut.Count.Should().Be(2);
        }

        [Fact]
        public void GetIndexedItems_ShouldReturnMetadataForAllItemsInTheIndex()
        {
            this.sut.GetIndexedItems().Should().BeEquivalentTo(
                new ItemMetadata<string>(0, "1", item1DocumentStatistics),
                new ItemMetadata<string>(1, "2", item2DocumentStatistics)
            );
        }

        [Fact]
        public void GetMetadataById_ShouldReturnCorrectItemForId()
        {
            this.sut.GetMetadata(this.id1).Should().BeEquivalentTo(new ItemMetadata<string>(this.id1, "1", item1DocumentStatistics));
            this.sut.GetMetadata(this.id2).Should().BeEquivalentTo(new ItemMetadata<string>(this.id2, "2", item2DocumentStatistics));
        }

        [Fact]
        public void GetMetadataById_ShouldThrowExceptionIfItemNotGound()
        {
            Assert.Throws<LiftiException>(() => this.sut.GetMetadata(this.id2 + 1))
                .Message.Should().Be("Item not found");
        }

        [Fact]
        public void ReleaseItem_ShouldReturnIdOfReleasedItem()
        {
            this.sut.ReleaseItem("2").Should().Be(this.id2);
            Assert.Throws<LiftiException>(() => this.sut.GetMetadata(this.id2));
        }

        [Fact]
        public void ReleasedItemId_ShouldBeReusedOnNextCreateId()
        {
            this.sut.ReleaseItem("1").Should().Be(this.id1);
            this.sut.ReleaseItem("2").Should().Be(this.id2);
            this.sut.Add("3", DocumentStatistics()).Should().Be(this.id1);
            this.sut.Add("4", DocumentStatistics()).Should().Be(this.id2);
            this.sut.Add("5", DocumentStatistics()).Should().Be(this.id2 + 1);
        }

        [Fact]
        public void Contains_WhenItemExists_ShouldReturnTrue()
        {
            this.sut.Contains("1").Should().BeTrue();
        }

        [Fact]
        public void Contains_WhenItemDoesntExist_ShouldReturnFalse()
        {
            this.sut.Contains("9").Should().BeFalse();
        }

        private static DocumentStatistics DocumentStatistics(params (byte fieldId, int wordCount)[] fieldWordCounts)
        {
            return new DocumentStatistics(fieldWordCounts.ToDictionary(f => f.fieldId, f => f.wordCount));
        }

        private static IndexStatistics IndexStatistics(params (byte fieldId, int wordCount)[] fieldWordCounts)
        {
            return new IndexStatistics(
                fieldWordCounts.ToImmutableDictionary(f => f.fieldId, f => (long)f.wordCount),
                fieldWordCounts.Sum(c => c.wordCount));
        }
    }
}
