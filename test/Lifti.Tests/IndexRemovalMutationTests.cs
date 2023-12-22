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

            VerifyResult(result, "www", expectedChildNodes: ['w']);
            VerifyResult(result, ['w'], "w");
        }

        [Fact]
        public void RemovingItemIdFromUnmutatedIndex_ShouldCauseItemToBeRemovedFromIndexAndChildNodes()
        {
            this.Sut.Add(Item1, FieldId1, new Token("www", this.Locations1.Locations));
            this.Sut.Add(Item1, FieldId1, new Token("wwwww", this.Locations2.Locations));

            this.ApplyMutationsToNewSut();

            this.Sut.Remove(Item1);

            var result = this.Sut.Apply();

            VerifyResult(result, "www", expectedChildNodes: ['w']);
            VerifyResult(result, ['w'], "w");
        }

        [Fact]
        public void RemovingItemIdFromUnmutatedIndex_ShouldNotAffectOtherItemsData()
        {
            this.Sut.Add(Item1, FieldId1, new Token("www", this.Locations1.Locations));
            this.Sut.Add(Item2, FieldId1, new Token("wwwww", this.Locations2.Locations));

            var result = this.ApplyMutationsToNewSut();

            VerifyResult(result, "www", expectedChildNodes: ['w'], expectedMatches: new[] { (Item1, this.Locations1) });
            VerifyResult(result, ['w'], "w", new[] { (Item2, this.Locations2) });

            this.Sut.Remove(Item1);

            result = this.Sut.Apply();

            // Item1 should be gone
            VerifyResult(result, "www", expectedChildNodes: ['w']);

            // But because we only removed Item1, Item2 should still be present
            VerifyResult(result, ['w'], "w", new[] { (Item2, this.Locations2) });
        }
    }
}
