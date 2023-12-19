using FluentAssertions;
using Lifti.Tokenization.Objects;
using System;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Lifti.Tests
{
    public class ItemStoreTests
    {
        private static readonly DocumentStatistics item1DocumentStatistics = DocumentStatistics((1, 100));
        private static readonly DocumentStatistics item2DocumentStatistics = DocumentStatistics((1, 50), (2, 200));

        private readonly ItemStore<string> sut;
        private readonly int id1;
        private readonly int id2;

        public ItemStoreTests()
        {
            this.sut = new ItemStore<string>(
                new[]
                {
                    new IndexedObjectConfiguration<string, string>(
                        1,
                        x => x,
                        Array.Empty<StaticFieldReader<string>>(),
                        Array.Empty<DynamicFieldReader<string>>(),
                        new ObjectScoreBoostOptions<string>(10D, null, 10D, null))
                });

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
        public void Add_ItemWithMatchingKey_ShouldThrowExceptionIfItemAlreadyIndexed()
        {
            Assert.Throws<LiftiException>(() => this.sut.Add(ItemMetadata(0)))
                .Message.Should().Be("Item already indexed");
        }

        [Fact]
        public void Add_ItemWithMatchingId_ShouldThrowExceptionIfIdAlreadyUsedAlreadyIndexed()
        {
            Assert.Throws<LiftiException>(() => this.sut.Add(ItemMetadata(1, key: "DifferentKey")))
                .Message.Should().Be("Id 1 is already registered in the index.");
        }

        [Fact]
        public void Add_ItemWithId_ShouldAddItemToIndex()
        {
            var documentStatistics = DocumentStatistics((1, 20), (2, 50), (3, 10));
            var itemMetadata = ItemMetadata<string>.ForObject(1, 9, "9", documentStatistics, new System.DateTime(2022, 11, 23), 12D);
            this.sut.Add(itemMetadata);
            this.sut.GetMetadata(9).Should().BeEquivalentTo(
                itemMetadata);
        }

        [Fact]
        public void Add_ItemWithId_ShouldAdjustIndexStatistics()
        {
            var documentStatistics = DocumentStatistics((1, 20), (2, 50), (3, 10));
            this.sut.Add(ItemMetadata(9, documentStatistics));
            this.sut.IndexStatistics.Should().BeEquivalentTo(
                IndexStatistics((1, 170), (2, 250), (3, 10)));
        }

        [Fact]
        public void Add_ItemWithId_ShouldResetTheNextIdBasedOnTheHighestIndexedId()
        {
            this.sut.Add(ItemMetadata(10, DocumentStatistics((10, 10))));
            this.sut.Add(ItemMetadata(9, DocumentStatistics((9, 9))));

            this.sut.Add("7", DocumentStatistics((7, 7)));

            this.sut.GetIndexedItems().Should().BeEquivalentTo(
                new[]
                {
                    ItemMetadata(0, item1DocumentStatistics),
                    ItemMetadata(1, item2DocumentStatistics),
                    ItemMetadata(9, DocumentStatistics((9, 9))),
                    ItemMetadata(10,DocumentStatistics((10, 10))),
                    ItemMetadata(11, DocumentStatistics((7, 7)), key: "7"),
                });
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
                new[]
                {
                    ItemMetadata(0, item1DocumentStatistics),
                    ItemMetadata(1, item2DocumentStatistics)
                });
        }

        [Fact]
        public void GetMetadataById_ShouldReturnCorrectItemForId()
        {
            this.sut.GetMetadata(this.id1).Should().BeEquivalentTo(ItemMetadata(this.id1, item1DocumentStatistics));
            this.sut.GetMetadata(this.id2).Should().BeEquivalentTo(ItemMetadata(this.id2, item2DocumentStatistics));
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
            this.sut.Remove("2").Should().Be(this.id2);
            Assert.Throws<LiftiException>(() => this.sut.GetMetadata(this.id2));
        }

        [Fact]
        public void ReleasedItemId_ShouldBeReusedOnNextCreateId()
        {
            this.sut.Remove("1").Should().Be(this.id1);
            this.sut.Remove("2").Should().Be(this.id2);
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

        [Fact]
        public void GetObjectTypeScoreBoostMetadata_WhenObjectIdExists_ShouldReturnMetadata()
        {
            this.sut.GetObjectTypeScoreBoostMetadata(1)
                .Should().NotBeNull();
        }

        [Fact]
        public void GetObjectTypeScoreBoostMetadata_WhenObjectIdDoesntExist_ShouldThrowException()
        {
            Assert.Throws<LiftiException>(() => this.sut.GetObjectTypeScoreBoostMetadata(2))
                .Message.Should().Be("Unknown object type id 2");
        }

        private static ItemMetadata<string> ItemMetadata(int id, DocumentStatistics? documentStatistics = null, string? key = null)
        {
            return ItemMetadata<string>.ForLooseText(id, key ?? (id + 1).ToString(), documentStatistics ?? DocumentStatistics());
        }

        private static DocumentStatistics DocumentStatistics(params (byte fieldId, int tokenCount)[] fieldWordCounts)
        {
            return new DocumentStatistics(fieldWordCounts.ToDictionary(f => f.fieldId, f => f.tokenCount));
        }

        private static IndexStatistics IndexStatistics(params (byte fieldId, int wordCount)[] fieldTokenCounts)
        {
            return new IndexStatistics(
                fieldTokenCounts.ToImmutableDictionary(f => f.fieldId, f => (long)f.wordCount),
                fieldTokenCounts.Sum(c => c.wordCount));
        }
    }
}
