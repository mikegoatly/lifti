using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Lifti
{
    /// <summary>
    /// An exception thrown by LIFTI.
    /// </summary>
    [Serializable]
    public class LiftiException : Exception
    {
        internal LiftiException() { }
        internal LiftiException(string message) : base(message) { }
        internal LiftiException(string message, params object[] args) : base(string.Format(CultureInfo.CurrentCulture, message, args)) { }
        internal LiftiException(string message, System.Exception inner) : base(message, inner) { }

        /// <inheritdoc />
        protected LiftiException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
    }
}
