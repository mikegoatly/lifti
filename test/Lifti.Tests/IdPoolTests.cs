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
            this.id1 = this.sut.CreateIdFor("1");
            this.id2 = this.sut.CreateIdFor("2");
        }

        [Fact]
        public void CreateIdFor_ShouldIncrementWhenNoItemsReturned()
        {
            this.id1.Should().BeLessThan(this.id2);
        }

        [Fact]
        public void CreateIdFor_ShouldThrowExceptionIfItemAlreadyIndexed()
        {
            Assert.Throws<LiftiException>(() => this.sut.CreateIdFor("1"))
                .Message.Should().Be("Item already indexed");
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
            this.sut.CreateIdFor("3").Should().Be(id1);
            this.sut.CreateIdFor("4").Should().Be(id2);
            this.sut.CreateIdFor("5").Should().Be(id2 + 1);
        }
    }
}
