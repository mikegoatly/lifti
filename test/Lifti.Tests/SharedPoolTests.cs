using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace Lifti.Tests
{
    public class SharedPoolTests
    {
        private SharedPool<List<string>> sut;

        public SharedPoolTests()
        {
            this.sut = new SharedPool<List<string>>(
                () => new List<string>(),
                l => l.Add("Returned"));
        }

        [Fact]
        public void WhenNoItemsInPool_ShouldCreateNew()
        {
            this.sut.Create().Should().NotBeNull();
        }

        [Fact]
        public void WhenItemInPool_ShouldReturnPooledItem()
        {
            var first = this.sut.Create();
            this.sut.Return(first);

            var second = this.sut.Create();
            first.Should().BeSameAs(second);
        }

        [Fact]
        public void WhenReturningItemToPool_ShouldApplyReturnFunction()
        {
            var first = this.sut.Create();
            first.Add("1");
            this.sut.Return(first);

            var second = this.sut.Create();
            second.Should().BeEquivalentTo("1", "Returned");
        }
    }
}
