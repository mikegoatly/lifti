using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace Lifti.Tests
{
    public class ListPoolTests
    {
        private readonly ListPool<string> sut;

        public ListPoolTests()
        {
            this.sut = new ListPool<string>(10, 5, 100);
        }

        [Fact]
        public void WhenNoItemsInPool_ShouldCreateNew()
        {
            var list = this.sut.Take();

            list.Should().NotBeNull();
            list.Capacity.Should().Be(10);
        }

        [Fact]
        public void WhenItemInPool_ShouldReturnPooledItem()
        {
            var first = this.sut.Take();
            this.sut.Return(first);

            var second = this.sut.Take();
            first.Should().BeSameAs(second);
        }

        [Fact]
        public void WhenReturningItemToPool_ShouldClearList()
        {
            var first = this.sut.Take();
            first.Add("1");
            this.sut.Return(first);

            var second = this.sut.Take();
            second.Should().BeEmpty();
        }

        [Fact]
        public void ShouldNotCacheMoreThanMaxPoolSize()
        {
            for (var i = 0; i < 7; i++)
            {
                this.sut.Return(new List<string>(50));
            }

            for (var i = 0; i < 5; i++)
            {
                this.sut.Take().Capacity.Should().Be(50);
            }

            this.sut.Take().Capacity.Should().Be(10);
        }

        [Fact]
        public void ShouldNotCacheItemOverMaxCapacity()
        {
            this.sut.Return(new List<string>(10000));

            this.sut.Take().Capacity.Should().Be(10);
        }
    }
}
