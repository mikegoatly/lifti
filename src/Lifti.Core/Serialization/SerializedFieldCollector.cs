namespace Lifti.Serialization
{
    /// <summary>
    /// Collects field metadata during a deserialization operation.
    /// </summary>
    public sealed class SerializedFieldCollector : DeserializedDataCollector<SerializedFieldInfo>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="SerializedFieldCollector"/> class.
        /// </summary>
        public SerializedFieldCollector()
            : base(10)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="SerializedFieldCollector"/> class.
        /// </summary>
        /// <param name="expectedCount">
        /// The expected number of fields to be collected.
        /// </param>
        public SerializedFieldCollector(int expectedCount)
            : base(expectedCount)
        {
        }
    }
}
