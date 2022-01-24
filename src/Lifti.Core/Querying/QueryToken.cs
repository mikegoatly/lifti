using System;

namespace Lifti.Querying
{
    /// <summary>
    /// A token parsed from a query string.
    /// </summary>
    public struct QueryToken : IEquatable<QueryToken>
    {
        private QueryToken(string tokenText, QueryTokenType tokenType, int tolerance)
        {
            this.TokenText = tokenText;
            this.TokenType = tokenType;
            this.Tolerance = tolerance;
        }

        /// <summary>
        /// The text that this instance represents. The value will be:
        /// - For text tokens: the text to search for.
        /// - For field filters: the name of the field to filter to. 
        /// - For operators: <see cref="string.Empty"/>.
        /// </summary>
        public string TokenText { get; }

        /// <summary>
        /// For operators that have tolerance, the number of tokens to use as the tolerance for the operator. For all other token types
        /// this field is ignored.
        /// </summary>
        public int Tolerance { get; }

        /// <summary>
        /// Gets the <see cref="QueryTokenType"/> that this instance represents.
        /// </summary>
        public QueryTokenType TokenType { get; }

        /// <summary>
        /// Creates a new <see cref="QueryToken"/> instance representing a textual part of the query.
        /// </summary>
        /// <param name="text">
        /// The text to be matched by the query.
        /// </param>
        public static QueryToken ForText(string text) => new QueryToken(text, QueryTokenType.Text, 0);

        /// <summary>
        /// Creates a new <see cref="QueryToken"/> instance representing a textual part of the query with fuzzy matching.
        /// </summary>
        /// <param name="text">
        /// The text to be matched by the query.
        /// </param>
        public static QueryToken ForFuzzyText(string text) => new QueryToken(text, QueryTokenType.FuzzyMatch, 3);

        /// <summary>
        /// Creates a new <see cref="QueryToken"/> instance representing a field filter.
        /// </summary>
        /// <param name="fieldName">
        /// The name of the field to match.
        /// </param>
        public static QueryToken ForFieldFilter(string fieldName) => new QueryToken(fieldName, QueryTokenType.FieldFilter, 0);

        /// <summary>
        /// Creates a new <see cref="QueryToken"/> instance representing a query operator.
        /// </summary>
        /// <param name="operatorType">
        /// The type of operator the token should represent.
        /// </param>
        public static QueryToken ForOperator(QueryTokenType operatorType) => new QueryToken(string.Empty, operatorType, 0);

        /// <summary>
        /// Creates a new <see cref="QueryToken"/> instance representing a query operator that has additional positional constraints.
        /// </summary>
        /// <param name="operatorType">
        /// The type of operator the token should represent.
        /// </param>
        /// <param name="tolerance">
        /// The number of tokens to use as the tolerance for the operator.
        /// </param>
        public static QueryToken ForOperatorWithTolerance(QueryTokenType operatorType, int tolerance) => 
            new QueryToken(string.Empty, operatorType, tolerance == 0 ? 5 : tolerance);

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is QueryToken token &&
                   this.Equals(token);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.TokenText, this.TokenType, this.Tolerance);
        }

        /// <inheritdoc />
        public static bool operator ==(QueryToken left, QueryToken right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc />
        public static bool operator !=(QueryToken left, QueryToken right)
        {
            return !(left == right);
        }

        /// <inheritdoc />
        public bool Equals(QueryToken other)
        {
            return this.TokenText == other.TokenText &&
                   this.TokenType == other.TokenType &&
                   this.Tolerance == other.Tolerance;
        }
    }

}
