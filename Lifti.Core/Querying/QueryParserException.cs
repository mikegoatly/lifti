using System;
using System.Globalization;

namespace Lifti.Querying
{

    [Serializable]
    public class QueryParserException : Exception
    {
        public QueryParserException() { }
        public QueryParserException(string message) : base(message) { }
        public QueryParserException(string message, params object[] formatParams) : base(string.Format(CultureInfo.CurrentCulture, message, formatParams)) { }
        public QueryParserException(string message, Exception inner) : base(message, inner) { }
        protected QueryParserException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
