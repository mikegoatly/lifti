using Lifti.Tokenization;
using Xunit;

namespace Lifti.Tests
{
    public class IndexRemovalMutationTests : MutationTestBase
    {
        [Fact]
        public void RemovingItemIdDuringMutation_ShouldCauseItemToBeRemovedFromIndexAndChildNodes()
        {
            this.Sut.Add(Item1, FieldId1, new Token("www", this.Locations1.Locations));
            this.Sut.Add(Item1, FieldId1, new Token("wwwww", this.Locations2.Locations));
            this.Sut.Remove(Item1);

            var result = this.Sut.Apply();

            VerifyResult(result, "www", expectedChildNodes: new[] { 'w' });
            VerifyResult(result, new[] { 'w' }, "w");
        }

        [Fact]
        public void RemovingItemIdFromUnmutatedIndex_ShouldCauseItemToBeRemovedFromIndexAndChildNodes()
        {
            this.Sut.Add(Item1, FieldId1, new Token("www", this.Locations1.Locations));
            this.Sut.Add(Item1, FieldId1, new Token("wwwww", this.Locations2.Locations));

            this.ApplyMutationsToNewSut();

            this.Sut.Remove(Item1);

            var result = this.Sut.Apply();

            VerifyResult(result, "www", expectedChildNodes: new[] { 'w' });
            VerifyResult(result, new[] { 'w' }, "w");
        }

        [Fact]
        public void RemovingItemIdFromUnmutatedIndex_ShouldNotAffectOtherItemsData()
        {
            this.Sut.Add(Item1, FieldId1, new Token("www", this.Locations1.Locations));
            this.Sut.Add(Item2, FieldId1, new Token("wwwww", this.Locations2.Locations));

            this.ApplyMutationsToNewSut();

            this.Sut.Remove(Item1);

            var result = this.Sut.Apply();

            VerifyResult(result, "www", expectedChildNodes: new[] { 'w' });
            VerifyResult(result, new[] { 'w' }, "w", new[] { (Item2, this.Locations2) });
        }
    }
}
