using System;
using System.Collections.Generic;

namespace Lifti.Querying
{
    public interface IIndexNavigator : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the navigator has matches exactly at its current position.
        /// </summary>
        bool HasExactMatches { get; }

        /// <summary>
        /// Enumerates all the tokens that are indexed under the current position in the navigator.
        /// </summary>
        IEnumerable<string> EnumerateIndexedTokens();

        /// <summary>
        /// Gets all the items that are indexed under from where the navigator is located.
        /// </summary>
        IntermediateQueryResult GetExactAndChildMatches();

        /// <summary>
        /// Gets all the items that are indexed exactly at the point of the navigators current location.
        /// </summary>
        IntermediateQueryResult GetExactMatches();

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
    }
}