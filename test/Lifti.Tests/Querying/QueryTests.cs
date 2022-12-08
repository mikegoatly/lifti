using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class QueryTests : IAsyncLifetime
    {
        private IFullTextIndex<string> index = null!;

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
        public void WithEmptyQueryPartRoot_ShouldReturnNoResults()
        {
            var query = new Query(EmptyQueryPart.Instance);
            query.Execute(this.index.Snapshot).Should().HaveCount(0);
        }

        [Fact]
        public void WithNullIndexPassed_ShouldThrowException()
        {
            var query = new Query(null!);
            Assert.Throws<ArgumentNullException>(() => query.Execute<string>(null!).ToList());
        }
    }
}
