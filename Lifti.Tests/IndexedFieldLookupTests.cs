using FluentAssertions;
using Xunit;

namespace Lifti.Tests
{
    public class IndexedFieldLookupTests
    {
        private readonly IndexedFieldLookup sut;

        public IndexedFieldLookupTests()
        {
            this.sut = new IndexedFieldLookup();
        }

        [Fact]
        public void GettingIdForNewFieldsShouldIncrementCounterEachTime()
        {
            this.sut.GetOrCreateIdForField("Field1").Should().Be(1);
            this.sut.GetOrCreateIdForField("Field2").Should().Be(2);
            this.sut.GetOrCreateIdForField("Field3").Should().Be(3);
        }

        [Fact]
        public void GettingIdForExistingFieldsShouldReturnSameIdEachTime()
        {
            this.sut.GetOrCreateIdForField("FieldX").Should().Be(1);
            this.sut.GetOrCreateIdForField("FieldY").Should().Be(2);
            this.sut.GetOrCreateIdForField("FieldX").Should().Be(1);
        }

        [Fact]
        public void GettingNameForValidIdShouldReturnCorrectFieldName()
        {
            this.sut.GetOrCreateIdForField("FieldX").Should().Be(1);
            this.sut.GetOrCreateIdForField("FieldY").Should().Be(2);

            this.sut.GetFieldForId(2).Should().Be("FieldY");
            this.sut.GetFieldForId(1).Should().Be("FieldX");
        }

        [Fact]
        public void GettingNameForInvalidIdShouldThrowException()
        {
            Assert.Throws<LiftiException>(() => this.sut.GetFieldForId(2))
                .Message.Should().Be("Field id 2 has no associated field name");
        }

        [Fact]
        public void GettingMoreThan255IdsShouldThrowException()
        {
            for (var i = 0; i < 254; i++)
            {
                this.sut.GetOrCreateIdForField("Field" + i).Should().Be((byte)(i + 1));
            }

            this.sut.GetOrCreateIdForField("Field255").Should().Be(255);

            Assert.Throws<LiftiException>(() => this.sut.GetOrCreateIdForField("Field256"))
                .Message.Should().Be("Only 255 distinct fields can currently be indexed");
        }
    }
}
