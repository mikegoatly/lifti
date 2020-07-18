using System.Collections.Generic;

namespace Lifti
{
    /// <summary>
    /// Search results for a specific field of a matched <see cref="SearchResult{T}"/>.
    /// </summary>
    public class FieldSearchResult
    {
        /// <summary>
        /// Constructs a new instance of <see cref="FieldSearchResult"/>.
        /// </summary>
        public FieldSearchResult(string foundIn, double score, IReadOnlyList<TokenLocation> locations)
        {
            this.FoundIn = foundIn;
            this.Score = score;
            this.Locations = locations;
        }

        /// <summary>
        /// Gets the name of the field that the search results were found in. 
        /// This will be one of the field names configured when the index was built, or 
        /// "Unspecified" if no fields were configured.
        /// </summary>
        public string FoundIn { get; }

        /// <summary>
        /// Gets the score for this particular field.
        /// </summary>
        public double Score { get; }

        /// <summary>
        /// Gets the <see cref="TokenLocation"/> instances for the locations of the matched tokens in the field.
        /// </summary>
        public IReadOnlyList<TokenLocation> Locations { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{FoundIn}: {string.Join(",", this.Locations)}";
        }
    }
}
