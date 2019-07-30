namespace Lifti
{
    [System.Serializable]
    public class LiftiException : System.Exception
    {
        public LiftiException() { }
        public LiftiException(string message) : base(message) { }
        public LiftiException(string message, System.Exception inner) : base(message, inner) { }
        protected LiftiException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
