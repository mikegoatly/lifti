using Lifti.Querying;

namespace Lifti.Tests.Fakes
{
    internal class FakeIndexSnapshot : IIndexSnapshot
    {
        public FakeIndexSnapshot(IIndexMetadata indexMetadata)
        {
            this.Metadata = indexMetadata;
        }

        public IndexNode Root => throw new System.NotImplementedException();

        public IIndexedFieldLookup FieldLookup => throw new System.NotImplementedException();

        public IIndexMetadata Items => this.Metadata;

        public IIndexMetadata Metadata { get; private set; }

        public IIndexNavigator CreateNavigator()
        {
            throw new System.NotImplementedException();
        }
    }
}
