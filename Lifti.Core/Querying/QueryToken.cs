using System;

namespace Lifti.Querying
{
    public struct QueryToken : IEquatable<QueryToken>
    {
        public QueryToken(string tokenText, QueryTokenType tokenType)
        {
            this.TokenText = tokenText;
            this.TokenType = tokenType;
        }

        public string TokenText { get; }

        public QueryTokenType TokenType { get; }

        public override bool Equals(object obj)
        {
            return obj is QueryToken token &&
                   this.Equals(token);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.TokenText, this.TokenType);
        }

        public static bool operator ==(QueryToken left, QueryToken right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QueryToken left, QueryToken right)
        {
            return !(left == right);
        }

        public bool Equals(QueryToken other)
        {
            return this.TokenText == other.TokenText &&
                   this.TokenType == other.TokenType;
        }
    }

}
