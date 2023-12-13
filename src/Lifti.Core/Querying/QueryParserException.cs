using System;
using System.Runtime.Serialization;

namespace Lifti.Querying
{
    /// <summary>
    /// A specialized exception thrown when an error occurs parsing a query.
    /// </summary>
    [Serializable]
    public class QueryParserException : LiftiException
    {
        /// <inheritdoc />
        public QueryParserException() { }

        /// <inheritdoc />
        public QueryParserException(string message) : base(message) { }

        /// <inheritdoc />
        public QueryParserException(string message, params object[] args) : base(message, args) { }

        /// <inheritdoc />
        public QueryParserException(string message, Exception inner) : base(message, inner) { }

#if NETSTANDARD
        /// <inheritdoc />
        protected QueryParserException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
#endif
    }
}
