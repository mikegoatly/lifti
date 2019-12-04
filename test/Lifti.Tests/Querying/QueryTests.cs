using FluentAssertions;
using Lifti.Querying;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class QueryTests : IAsyncLifetime
    {
        private IFullTextIndex<string> index;

        public async Task InitializeAsync()
        {
            this.index = new FullTextIndexBuilder<string>().Build();
            await this.index.AddAsync("A", "Some test text");
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public void WithNullRoot_ShouldReturnNoResults()
        {
            var query = new Query(null);
            query.Execute(this.index.Snapshot).Should().HaveCount(0);
        }

        [Fact]
        public void WithNullIndexPassed_ShouldThrowException()
        {
            var query = new Query(null);
            Assert.Throws<ArgumentNullException>(() => query.Execute<string>(null).ToList());
        }
    }
}
