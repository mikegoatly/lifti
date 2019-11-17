namespace Lifti
{
    internal abstract class IndexMutation : IIndexMutation
    {
        protected IndexMutation(IndexNode root, IIndexNodeFactory indexNodeFactory)
        {
            this.Root = new IndexNodeMutation(0, root, indexNodeFactory);
        }

        protected IndexNodeMutation Root { get; }

        public IndexNode ApplyMutations()
        {
            return this.Root.Apply();
        }
    }
}
