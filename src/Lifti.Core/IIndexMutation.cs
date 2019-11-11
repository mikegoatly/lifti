namespace Lifti
{
    internal interface IIndexMutation
    {
        IndexNode ApplyMutations();
        void TrackMutatedNode(IndexNodeMutation mutatedNode);
    }
}