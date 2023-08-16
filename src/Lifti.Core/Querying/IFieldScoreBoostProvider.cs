namespace Lifti.Querying
{
    /// <summary>
    /// An interface for a provider that can provide a score boost for a field.
    /// </summary>
    internal interface IFieldScoreBoostProvider
    {   
        /// <summary>
        /// Gets the boost for the specified field.
        /// </summary>
        double GetScoreBoost(byte fieldId);
    }
}
