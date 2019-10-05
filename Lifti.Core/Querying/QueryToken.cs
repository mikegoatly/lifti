using System;

namespace Lifti.Querying
{
    public struct QueryToken : IEquatable<QueryToken>
    {
        private QueryToken(string tokenText, QueryTokenType tokenType, int tolerance)
        {
            this.TokenText = tokenText;
            this.TokenType = tokenType;
            this.Tolerance = tolerance;
        }

        public string TokenText { get; }
        
        public int Tolerance { get; }

        public QueryTokenType TokenType { get; }

        public static QueryToken ForWord(string text) => new QueryToken(text, QueryTokenType.Text, 0);
        public static QueryToken ForFieldFilter(string text) => new QueryToken(text, QueryTokenType.FieldFilter, 0);
        public static QueryToken ForOperator(QueryTokenType operatorType) => new QueryToken(null, operatorType, 0);
        public static QueryToken ForOperatorWithTolerance(QueryTokenType operatorType, int tolerance) => 
            new QueryToken(null, operatorType, tolerance == 0 ? 5 : tolerance);

        public override bool Equals(object obj)
        {
            return obj is QueryToken token &&
                   this.Equals(token);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.TokenText, this.TokenType, this.Tolerance);
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
                   this.TokenType == other.TokenType &&
                   this.Tolerance == other.Tolerance;
        }
    }

}
