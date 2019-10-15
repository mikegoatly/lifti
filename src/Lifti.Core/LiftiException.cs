using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Lifti
{
    [Serializable]
    public class LiftiException : Exception
    {
        public LiftiException() { }
        public LiftiException(string message) : base(message) { }
        public LiftiException(string message, params object[] args) : base(string.Format(CultureInfo.CurrentCulture, message, args)) { }
        public LiftiException(string message, System.Exception inner) : base(message, inner) { }
        protected LiftiException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
    }
}
