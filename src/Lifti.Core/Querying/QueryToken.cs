using Lifti.Tokenization;
using System;

namespace Lifti.Querying
{
    /// <summary>
    /// A token parsed from a query string.
    /// </summary>
    internal class QueryToken : IEquatable<QueryToken>
    {
        private QueryToken(
            string tokenText,
            QueryTokenType tokenType,
            int tolerance,
            IIndexTokenizer? indexTokenizer,
            double? scoreBoost = null)
        {
            this.TokenText = tokenText;
            this.TokenType = tokenType;
            this.Tolerance = tolerance;
            this.ScoreBoost = scoreBoost;
            this.IndexTokenizer = indexTokenizer;
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
        /// The score boost to apply to any items matching the search term. This is multiplied with any score boosts
        /// applied to matching fields. A null value indicates that no additional score boost should be applied.
        /// </summary>
        public double? ScoreBoost { get; }

        /// <summary>
        /// The <see cref="IIndexTokenizer"/> to use when further tokenizing the text in this instance.
        /// </summary>
        public IIndexTokenizer? IndexTokenizer { get; }

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
        /// <param name="indexTokenizer">
        /// The <see cref="IIndexTokenizer"/> to use when further tokenizing the captured text. 
        /// </param>
        /// <param name="scoreBoost">
        /// The score boost to apply to any items matching the search term. This is multiplied with any score boosts
        /// applied to matching fields. A null value indicates that no additional score boost should be applied.
        /// </param>
        public static QueryToken ForText(string text, IIndexTokenizer indexTokenizer, double? scoreBoost)
        {
            return new(text, QueryTokenType.Text, 0, indexTokenizer, scoreBoost);
        }

        /// <summary>
        /// Creates a new <see cref="QueryToken"/> instance representing a field filter.
        /// </summary>
        /// <param name="fieldName">
        /// The name of the field to match.
        /// </param>
        public static QueryToken ForFieldFilter(string fieldName)
        {
            return new(fieldName, QueryTokenType.FieldFilter, 0, null);
        }

        /// <summary>
        /// Creates a new <see cref="QueryToken"/> instance representing a query operator.
        /// </summary>
        /// <param name="operatorType">
        /// The type of operator the token should represent.
        /// </param>
        public static QueryToken ForOperator(QueryTokenType operatorType)
        {
            return new(string.Empty, operatorType, 0, null);
        }

        /// <summary>
        /// Creates a new <see cref="QueryToken"/> instance representing a query operator that has additional positional constraints.
        /// </summary>
        /// <param name="operatorType">
        /// The type of operator the token should represent.
        /// </param>
        /// <param name="tolerance">
        /// The number of tokens to use as the tolerance for the operator.
        /// </param>
        public static QueryToken ForOperatorWithTolerance(QueryTokenType operatorType, int tolerance)
        {
            return new(string.Empty, operatorType, tolerance == 0 ? 5 : tolerance, null, null);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
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
        public bool Equals(QueryToken? other)
        {
            if (other is null)
            {
                return false;
            }

            return this.TokenText == other.TokenText &&
                   this.TokenType == other.TokenType &&
                   this.Tolerance == other.Tolerance;
        }
    }

}
