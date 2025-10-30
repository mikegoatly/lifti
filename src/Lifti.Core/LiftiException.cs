using System;
using System.Globalization;

namespace Lifti
{
    /// <summary>
    /// An exception thrown by LIFTI.
    /// </summary>
    [Serializable]
    public class LiftiException : Exception
    {
        /// <inheritdoc />
        public LiftiException() { }

        /// <inheritdoc />
        public LiftiException(string message) : base(message) { }

        /// <inheritdoc />
        public LiftiException(string message, params object[] args) : base(string.Format(CultureInfo.CurrentCulture, message, args)) { }

        /// <inheritdoc />
        public LiftiException(string message, System.Exception inner) : base(message, inner) { }
    }
}
