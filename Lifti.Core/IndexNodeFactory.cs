namespace Lifti
{
    public class IndexNodeFactory : IIndexNodeFactory
    {
        private int supportIntraNodeTextAtDepth;

        public IndexNode CreateNode()
        {
            return new IndexNode(this, 0, this.GetIndexSupportLevelForDepth(0));
        }

        private IndexSupportLevelKind GetIndexSupportLevelForDepth(int depth)
        {
            return depth >= this.supportIntraNodeTextAtDepth ?
                IndexSupportLevelKind.IntraNodeText :
                IndexSupportLevelKind.CharacterByCharacter;
        }

        public IndexNode CreateNode(IndexNode parent)
        {
            var nextDepth = parent.Depth + 1;
            return new IndexNode(this, nextDepth, this.GetIndexSupportLevelForDepth(nextDepth));
        }

        public virtual void ConfigureWith(FullTextIndexOptions options)
        {
            this.supportIntraNodeTextAtDepth = options.Advanced.SupportIntraNodeTextAfterCharacterIndex;
        }
    }
}
