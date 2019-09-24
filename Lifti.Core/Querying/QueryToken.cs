namespace Lifti.Querying
{
    public struct QueryToken
    {
        public QueryToken(string tokenText, QueryTokenType tokenType)
        {
            this.TokenText = tokenText;
            this.TokenType = tokenType;
        }

        public string TokenText { get; }

        public QueryTokenType TokenType { get; }
    }

}
