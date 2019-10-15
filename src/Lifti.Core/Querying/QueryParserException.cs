using System;
using System.Runtime.Serialization;

namespace Lifti.Querying
{
    [Serializable]
    public class QueryParserException : LiftiException
    {
        public QueryParserException() { }
        public QueryParserException(string message) : base(message) { }
        public QueryParserException(string message, params object[] args) : base(message, args) { }
        public QueryParserException(string message, Exception inner) : base(message, inner) { }
        protected QueryParserException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
    }
}
