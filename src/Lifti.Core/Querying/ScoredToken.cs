namespace Lifti.Querying
{
    public struct ScoredToken
    {
        public ScoredToken(QueryWordMatch match, double score)
        {
            this.Match = match;
            this.Score = score;
        }

        public QueryWordMatch Match { get; }
        public double Score { get; }
    }
}
