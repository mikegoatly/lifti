namespace Lifti.Querying
{
    /// <summary>
    /// Implemented by classes capable of providing the score boost for a given field.
    /// </summary>
    public interface IFieldScoreBoostProvider
    {
        /// <summary>
        /// Gets the boost for the specified field.
        /// </summary>
        double GetScoreBoost(byte fieldId);
    }
}
