using System.Collections.Generic;

namespace Lifti.Querying
{
    /// <summary>
    /// Provides methods for scoring search results.
    /// </summary>
    public interface IScorer
    {
        /// <summary>
        /// Calculates the score for a match in a single document's field.
        /// </summary>
        /// <param name="totalMatchedDocuments">
        /// The total number of documents that matched the query.
        /// </param>
        /// <param name="documentId">
        /// The id of the document that the match is in.
        /// </param>
        /// <param name="fieldId">
        /// The id of the field that the match is in.
        /// </param>
        /// <param name="tokenLocations">
        /// The complete set of locations that the token was found in the document.
        /// </param>
        /// <param name="weighting">
        /// The weighting multiplier to apply to the score.
        /// </param>
        /// <returns>
        /// The score for the match.
        /// </returns>
        double CalculateScore(int totalMatchedDocuments, int documentId, byte fieldId, IReadOnlyList<TokenLocation> tokenLocations, double weighting);
    }
}
