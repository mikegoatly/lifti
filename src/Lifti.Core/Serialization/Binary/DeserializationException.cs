using System;
using System.Runtime.Serialization;

namespace Lifti.Serialization.Binary
{
    /// <summary>
    /// An exception thrown while deserializing an index.
    /// </summary>
    [Serializable]
    public sealed class DeserializationException : LiftiException
    {
        /// <inheritdoc/>
        public DeserializationException() { }

        /// <inheritdoc/>
        public DeserializationException(string message) : base(message) { }

        /// <inheritdoc/>
        public DeserializationException(string message, params object[] args) : base(message, args) { }

        /// <inheritdoc/>
        public DeserializationException(string message, Exception inner) : base(message, inner) { }

#if NETSTANDARD
        private DeserializationException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
#endif
    }
}
