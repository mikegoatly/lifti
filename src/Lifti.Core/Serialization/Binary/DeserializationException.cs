using System;
using System.Runtime.Serialization;

namespace Lifti.Serialization.Binary
{
    [Serializable]
    public sealed class DeserializationException : LiftiException
    {
        public DeserializationException() { }
        public DeserializationException(string message) : base(message) { }
        public DeserializationException(string message, params object[] args) : base(message, args) { }
        public DeserializationException(string message, Exception inner) : base(message, inner) { }
        private DeserializationException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
    }
}
