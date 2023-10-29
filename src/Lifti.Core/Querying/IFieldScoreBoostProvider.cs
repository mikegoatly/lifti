namespace Lifti.Querying
{
    /// <summary>
    /// A provider capable of providing the score boost for a given field.
    /// </summary>
    internal interface IFieldScoreBoostProvider
    {
        /// <summary>
        /// Gets the boost for the specified field.
        /// </summary>
        double GetScoreBoost(byte fieldId);
    }
}
