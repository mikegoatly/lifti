using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// An <see cref="IQueryPart"/> that matches documents where the word appears
    /// at the start and/or end of a field based on token position.
    /// </summary>
    public sealed class AnchoredWordQueryPart : WordQueryPart
    {
        /// <summary>
        /// Constructs a new instance of <see cref="AnchoredWordQueryPart"/>.
        /// </summary>
        /// <param name="word">The word to search for in the index.</param>
        /// <param name="requireStart">If true, the word must appear at the start of the field (token index 0).</param>
        /// <param name="requireEnd">If true, the word must appear at the end of the field (last token).</param>
        /// <param name="scoreBoost">Optional score boost to apply to matches.</param>
        public AnchoredWordQueryPart(
            string word,
            bool requireStart,
            bool requireEnd,
            double? scoreBoost = null)
            : base(word, scoreBoost)
        {
            if (requireStart == false && requireEnd == false)
            {
                throw new ArgumentException(ExceptionMessages.MustAnchorAtStartEndOrBoth);
            }

            this.RequireStart = requireStart;
            this.RequireEnd = requireEnd;
        }

        /// <summary>
        /// Gets a value indicating whether the word must appear at the start of the field.
        /// </summary>
        public bool RequireStart { get; }

        /// <summary>
        /// Gets a value indicating whether the word must appear at the end of the field.
        /// </summary>
        public bool RequireEnd { get; }

        /// <inheritdoc/>
        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext)
        {
            ArgumentNullException.ThrowIfNull(navigatorCreator);

            ArgumentNullException.ThrowIfNull(queryContext);

            var timing = queryContext.ExecutionTimings.Start(this, queryContext);
            using var navigator = navigatorCreator();
            navigator.Process(this.Word.AsSpan());
            var results = navigator.GetExactMatches(queryContext, this.ScoreBoost ?? 1D);

            // Filter results based on anchor requirements
            var filteredResults = this.FilterByAnchors(results, navigator.Snapshot.Metadata);

            return timing.Complete(filteredResults);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var anchor = (this.RequireStart, this.RequireEnd) switch
            {
                (true, true) => $"<<{this.Word}>>",
                (true, false) => $"<<{this.Word}",
                (false, true) => $"{this.Word}>>",
                _ => this.Word
            };

            return base.ToString(anchor);
        }

        private IntermediateQueryResult FilterByAnchors(IntermediateQueryResult results, IIndexMetadata metadata)
        {
            if (results.Matches.Count == 0)
            {
                return results;
            }

            var filteredMatches = new List<ScoredToken>();

            foreach (var match in results.Matches)
            {
                var filteredFieldMatches = new List<ScoredFieldMatch>();

                foreach (var fieldMatch in match.FieldMatches)
                {
                    var fieldId = fieldMatch.FieldId;
                    var documentMetadata = metadata.GetDocumentMetadata(match.DocumentId);

                    // Get the last token index for this field
                    if (!documentMetadata.DocumentStatistics.StatisticsByField.TryGetValue(fieldId, out var fieldStats))
                    {
                        // Field not indexed in this document, skip
                        continue;
                    }

                    var lastTokenIndex = fieldStats.LastTokenIndex;

                    // Check if we have the required metadata for exact match queries
                    if (lastTokenIndex < 0)
                    {
                        throw new LiftiException(ExceptionMessages.MissingLastTokenIndexMetadata);
                    }

                    // Filter locations based on anchor requirements
                    var filteredLocations = new List<ITokenLocation>();
                    foreach (var location in fieldMatch.Locations)
                    {
                        // For single token locations, MinTokenIndex equals the TokenIndex
                        // For composite locations, we check if any token in the range matches
                        if (this.IsAnchorMatch(location.MinTokenIndex, location.MaxTokenIndex, lastTokenIndex))
                        {
                            filteredLocations.Add(location);
                        }
                    }

                    if (filteredLocations.Count > 0)
                    {
                        filteredFieldMatches.Add(
                            ScoredFieldMatch.CreateFromPresorted(
                                fieldMatch.Score,
                                fieldId,
                                filteredLocations));
                    }
                }

                if (filteredFieldMatches.Count > 0)
                {
                    filteredMatches.Add(new ScoredToken(match.DocumentId, filteredFieldMatches));
                }
            }

            return new IntermediateQueryResult(filteredMatches, false);
        }

        private bool IsAnchorMatch(int minTokenIndex, int maxTokenIndex, int lastTokenIndex)
        {
            if (this.RequireStart)
            {
                if (this.RequireEnd)
                {
                    // Both anchors: the token(s) must include index 0 AND the last token
                    return minTokenIndex == 0 && maxTokenIndex == lastTokenIndex;
                }

                // Start anchor only: must include index 0
                return minTokenIndex == 0;
            }

            // End anchor only: must include the last token index
            return maxTokenIndex == lastTokenIndex;
        }
    }
}
