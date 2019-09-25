using FluentAssertions;
using Lifti.Querying;
using System;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class QueryTests
    {
        private readonly FullTextIndex<string> index;

        public QueryTests()
        {
            this.index = new FullTextIndex<string>();
            this.index.Index("A", "Some test text");
        }

        [Fact]
        public void WithNullRoot_ShouldReturnNoResults()
        {
            var query = new Query(null);
            query.Execute(this.index).Should().HaveCount(0);
        }

        [Fact]
        public void WithNullIndexPassed_ShouldThrowException()
        {
            var query = new Query(null);
            Assert.Throws<ArgumentNullException>(() => query.Execute<string>(null).ToList());
        }
    }
}
