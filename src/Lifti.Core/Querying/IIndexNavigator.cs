using Lifti.Querying.Lifti.Querying;
using System;
using System.Collections.Generic;

namespace Lifti.Querying
{
    /// <summary>
    /// Defines methods for navigating an index character by character.
    /// </summary>
    public interface IIndexNavigator : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the navigator has matches exactly at its current position.
        /// </summary>
        bool HasExactMatches { get; }

        /// <summary>
        /// Gets the index snapshot the navigator is currently navigating.
        /// </summary>
        IIndexSnapshot Snapshot { get; }

        /// <summary>
        /// Enumerates all the tokens that are indexed under the current position in the navigator. This method can be used
        /// to reverse-engineer the words (tokens) that have been indexed. Note that this method will throw a <see cref="LiftiException"/>
        /// if called after a bookmark obtained by <see cref="CreateBookmark"/> is applied.
        /// </summary>
        IEnumerable<string> EnumerateIndexedTokens();

        /// <summary>
        /// Gets all the matches that are indexed under from where the navigator is located.
        /// </summary>
        /// <param name="queryContext">
        /// The current query context.
        /// </param>
        /// <param name="documentMatchCollector">
        /// The document match collector to add the matches to.
        /// </param>
        /// <param name="weighting">
        /// The weighting to apply to the matched tokens. This can be used to adjust the resulting score for the match.
        /// </param>
        void AddExactAndChildMatches(QueryContext queryContext, DocumentMatchCollector documentMatchCollector, double weighting = 1D);

        /// <summary>
        /// Gets all the matches that are indexed exactly at the point of the navigators current location.
        /// </summary>
        /// <param name="queryContext">
        /// The current query context.
        /// </param>
        /// <param name="documentMatchCollector">
        /// The document match collector to add the matches to.
        /// </param>
        /// <param name="weighting">
        /// The weighting to apply to the matched tokens. This can be used to adjust the resulting score for the match.
        /// </param>
        void AddExactMatches(QueryContext queryContext, DocumentMatchCollector documentMatchCollector, double weighting = 1D);

        /// <summary>
        /// Gets all the matches that are indexed under from where the navigator is located.
        /// </summary>
        /// <param name="weighting">
        /// The weighting to apply to the matched tokens. This can be used to adjust the resulting score for the match.
        /// </param>
        IntermediateQueryResult GetExactAndChildMatches(double weighting = 1D);

        /// <summary>
        /// Gets all the matches that are indexed exactly at the point of the navigators current location.
        /// </summary>
        /// <param name="weighting">
        /// The weighting to apply to the matched tokens. This can be used to adjust the resulting score for the match.
        /// </param>
        IntermediateQueryResult GetExactMatches(double weighting = 1D);

        /// <summary>
        /// Gets all the matches that are indexed under from where the navigator is located.
        /// </summary>
        /// <param name="queryContext">
        /// The current query context.
        /// </param>
        /// <param name="weighting">
        /// The weighting to apply to the matched tokens. This can be used to adjust the resulting score for the match.
        /// </param>
        IntermediateQueryResult GetExactAndChildMatches(QueryContext queryContext, double weighting = 1D);

        /// <summary>
        /// Gets all the matches that are indexed exactly at the point of the navigators current location.
        /// </summary>
        /// <param name="queryContext">
        /// The current query context.
        /// </param>
        /// <param name="weighting">
        /// The weighting to apply to the matched tokens. This can be used to adjust the resulting score for the match.
        /// </param>
        IntermediateQueryResult GetExactMatches(QueryContext queryContext, double weighting = 1D);

        /// <summary>
        /// Processes a single character, moving the navigator along the index.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the navigator could process the character, otherwise <c>false</c>.
        /// </returns>
        bool Process(char value);

        /// <summary>
        /// Processes a string of characters, moving the navigator along the index with each of them in turn.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the navigator could process all the characters in the string, otherwise <c>false</c>.
        /// </returns>
        bool Process(ReadOnlySpan<char> text);

        /// <inheritdoc cref="Process(ReadOnlySpan{char})"/>
        bool Process(string text);

        /// <summary>
        /// Creates an <see cref="IIndexNavigatorBookmark"/> for the current state of this instance.
        /// </summary>
        IIndexNavigatorBookmark CreateBookmark();

        /// <summary>
        /// Enumerates all the characters that are available as options to process from the navigators current location.
        /// </summary>
        IEnumerable<char> EnumerateNextCharacters();

        /// <summary>
        /// Gets the number of exact matches that are indexed at the current location.
        /// </summary>
        int ExactMatchCount();
    }
}