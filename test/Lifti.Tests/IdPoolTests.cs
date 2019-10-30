using FluentAssertions;
using Xunit;

namespace Lifti.Tests
{
    public class IdPoolTests
    {
        private readonly IdPool<string> sut;
        private readonly int id1;
        private readonly int id2;

        public IdPoolTests()
        {
            this.sut = new IdPool<string>();
            this.id1 = this.sut.Add("1");
            this.id2 = this.sut.Add("2");
        }

        [Fact]
        public void Add_ItemOnly_ShouldIncrementWhenNoItemsReturned()
        {
            this.id1.Should().BeLessThan(this.id2);
        }

        [Fact]
        public void Add_ItemOnly_ShouldThrowExceptionIfItemAlreadyIndexed()
        {
            Assert.Throws<LiftiException>(() => this.sut.Add("1"))
                .Message.Should().Be("Item already indexed");
        }

        [Fact]
        public void Add_ItemWithId_ShouldThrowExceptionIfItemAlreadyIndexed()
        {
            Assert.Throws<LiftiException>(() => this.sut.Add(9, "1"))
                .Message.Should().Be("Item already indexed");
        }

        [Fact]
        public void Add_ItemWithId_ShouldThrowExceptionIfIdAlreadyUsedAlreadyIndexed()
        {
            Assert.Throws<LiftiException>(() => this.sut.Add(1, "9"))
                .Message.Should().Be("Id 1 is already registered in the index.");
        }

        [Fact]
        public void Add_ItemWithId_ShouldAddItemToIndex()
        {
            this.sut.Add(9, "9");
            this.sut.GetItemForId(9).Should().Be("9");
        }

        [Fact]
        public void Add_ItemWithId_ShouldResetTheNextIdBasedOnTheHighestIndexedId()
        {
            this.sut.Add(10, "10");
            this.sut.Add(9, "9");

            this.sut.Add("7");

            this.sut.GetIndexedItems().Should().BeEquivalentTo(
                ("1", 0),
                ("2", 1),
                ("9", 9),
                ("10", 10),
                ("7", 11)
            );
        }

        [Fact]
        public void Count_ShouldReturnCorrectValue()
        {
            this.sut.Count.Should().Be(2);
        }

        [Fact]
        public void GetIndexedItems_ShouldReturnItemsInTheIndexWithAssociatedIds()
        {
            this.sut.GetIndexedItems().Should().BeEquivalentTo(
                ("1", 0),
                ("2", 1)
            );
        }

        [Fact]
        public void GetItemForId_ShouldReturnCorrectItemForId()
        {
            this.sut.GetItemForId(this.id1).Should().Be("1");
            this.sut.GetItemForId(this.id2).Should().Be("2");
        }

        [Fact]
        public void GetItemForId_ShouldThrowExceptionIfItemNotGound()
        {
            Assert.Throws<LiftiException>(() => this.sut.GetItemForId(this.id2 + 1))
                .Message.Should().Be("Item not found");
        }

        [Fact]
        public void ReleaseItem_ShouldReturnIdOfReleasedItem()
        {
            this.sut.ReleaseItem("2").Should().Be(id2);
            Assert.Throws<LiftiException>(() => this.sut.GetItemForId(this.id2));
        }

        [Fact]
        public void ReleasedItemId_ShouldBeReusedOnNextCreateId()
        {
            this.sut.ReleaseItem("1").Should().Be(id1);
            this.sut.ReleaseItem("2").Should().Be(id2);
            this.sut.Add("3").Should().Be(id1);
            this.sut.Add("4").Should().Be(id2);
            this.sut.Add("5").Should().Be(id2 + 1);
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
    }
}
