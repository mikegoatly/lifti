﻿using System;
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
        /// Enumerates all the tokens that are indexed under the current position in the navigator. This method can be used
        /// to reverse-engineer the words (tokens) that have been indexed. Note that this method will throw a <see cref="LiftiException"/>
        /// if called after a bookmark obtained by <see cref="CreateBookmark"/> is applied.
        /// </summary>
        IEnumerable<string> EnumerateIndexedTokens();

        /// <summary>
        /// Gets all the items that are indexed under from where the navigator is located.
        /// </summary>
        /// <param name="weighting">
        /// The weighting to apply to the matched tokens. This can be used to adjust the resulting score for the match.
        /// </param>
        IntermediateQueryResult GetExactAndChildMatches(double weighting = 1D);

        /// <summary>
        /// Gets all the items that are indexed exactly at the point of the navigators current location.
        /// </summary>
        /// <param name="weighting">
        /// The weighting to apply to the matched tokens. This can be used to adjust the resulting score for the match.
        /// </param>
        IntermediateQueryResult GetExactMatches(double weighting = 1D);

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

        /// <summary>
        /// Creates an <see cref="IIndexNavigatorBookmark"/> for the current state of this instance.
        /// </summary>
        IIndexNavigatorBookmark CreateBookmark();
        
        /// <summary>
        /// Enumerates all the characters that are available as options to process from the navigators current location.
        /// </summary>
        IEnumerable<char> EnumerateNextCharacters();
    }
}