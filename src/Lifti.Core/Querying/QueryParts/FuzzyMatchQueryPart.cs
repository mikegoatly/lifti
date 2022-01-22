#define TRACK_MATCH_STATE_TEXT
// Enabling TRACK_MATCH_STATE_TEXT in DEBUG builds will allow you inspect the state of a fuzzy match
// as it is being processed using the MatchText property. This is not available in release builds for performance.

using System;
using System.Collections.Generic;

namespace Lifti.Querying.QueryParts
{
    internal struct SubstitutedCharacters
    {
        public SubstitutedCharacters(char expected, char replacedWith)
        {
            this.Expected = expected;
            this.ReplacedWith = replacedWith;
        }

        public char Expected { get; }
        public char ReplacedWith { get; }
    }

    /// <summary>
    /// An <see cref="IQueryPart"/> that matches items that contain an fuzzy match for the given text.
    /// </summary>
    public class FuzzyMatchQueryPart : WordQueryPart
    {
        private readonly int maxEditDistance;

        private enum EditKind
        {
            Substitution,
            Deletion
        }

        private class FuzzyMatchStateStore
        {
            private readonly int maxEditDistance;
            private readonly DoubleBufferedList<FuzzyMatchState> state;

            // It's very likely that we'll encounter the same point in the index multiple times while processing a fuzzy match.
            // There's no point in traversing the same part multiple times for a given point in the search term, so this hashset keeps track of each logical
            // location that has been reached at each index within the search term.
            private readonly HashSet<(int wordIndex, IIndexNavigatorBookmark bookmark)> processedBookmarks = new HashSet<(int, IIndexNavigatorBookmark)>();

            public FuzzyMatchStateStore(IIndexNavigator navigator, int maxEditDistance)
            {
                this.state = new DoubleBufferedList<FuzzyMatchState>(new FuzzyMatchState(navigator.CreateBookmark()));
                this.maxEditDistance = maxEditDistance;
            }

            public bool HasEntries => this.state.Count > 0;

            public void Add(FuzzyMatchState state)
            {
                if (state.EditDistance <= maxEditDistance && processedBookmarks.Add((state.WordIndex, state.Bookmark)))
                {
                    this.state.Add(state);
                }
            }

            public IEnumerable<FuzzyMatchState> GetNextStateEntries()
            {
                return this.state;
            }

            public void PrepareNextEntries()
            {
                this.state.Swap();
            }
        }

        private struct FuzzyMatchState
        {
            public FuzzyMatchState(
                IIndexNavigatorBookmark bookmark,
                int editCount,
                int wordIndex
#if DEBUG && TRACK_MATCH_STATE_TEXT
                , string matchText
#endif
                )
            {
                this.Bookmark = bookmark;
                this.EditDistance = editCount;
                this.WordIndex = wordIndex;
#if DEBUG && TRACK_MATCH_STATE_TEXT
                this.MatchText = matchText;
#endif
            }

            public FuzzyMatchState(IIndexNavigatorBookmark bookmark)
                : this(
                      bookmark,
                      0,
                      0
#if DEBUG && TRACK_MATCH_STATE_TEXT
                      , string.Empty
#endif
                      )
            {
            }

            public IIndexNavigatorBookmark Bookmark { get; }

            public int EditDistance { get; }

            /// <summary>
            /// The index that this state is currently matching in the target word.
            /// </summary>
            public int WordIndex { get; }

#if DEBUG && TRACK_MATCH_STATE_TEXT
            public string MatchText { get; }
#endif

            public FuzzyMatchState ApplySubstitution(IIndexNavigatorBookmark newBookmark, SubstitutedCharacters substituted)
            {
                return new FuzzyMatchState(
                    newBookmark,
                    this.EditDistance + 1,
                    this.WordIndex + 1,
#if DEBUG && TRACK_MATCH_STATE_TEXT
                    this.MatchText + $"(-{substituted.Expected}+{substituted.ReplacedWith})"
#endif
                    );
            }

            public FuzzyMatchState ApplyDeletion(IIndexNavigatorBookmark newBookmark, char omittedCharacter)
            {
                return new FuzzyMatchState(
                    newBookmark,
                    this.EditDistance + 1,
                    this.WordIndex,
#if DEBUG && TRACK_MATCH_STATE_TEXT
                    this.MatchText + $"(-{omittedCharacter})"
#endif
                    );
            }

            public FuzzyMatchState ApplyInsertion(char additionalCharacter)
            {
                return new FuzzyMatchState(
                    this.Bookmark,
                    this.EditDistance + 1,
                    this.WordIndex + 1,
#if DEBUG && TRACK_MATCH_STATE_TEXT
                    this.MatchText + $"(+{additionalCharacter})"
#endif
                    );
            }

            public FuzzyMatchState ApplyExactMatch(IIndexNavigatorBookmark nextBookmark, char currentCharacter)
            {
                return new FuzzyMatchState(
                    nextBookmark,
                    this.EditDistance,
                    this.WordIndex + 1,
#if DEBUG && TRACK_MATCH_STATE_TEXT
                    this.MatchText + currentCharacter
#endif
                    );
            }
        }

        /// <summary>
        /// Constructs a new instance of <see cref="FuzzyMatchQueryPart"/>.
        /// </summary>
        public FuzzyMatchQueryPart(string word, int maxEditDistance)
            : base(word)
        {
            this.maxEditDistance = maxEditDistance;
        }

        /// <inheritdoc/>
        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, IQueryContext queryContext)
        {
            if (navigatorCreator == null)
            {
                throw new ArgumentNullException(nameof(navigatorCreator));
            }

            using var navigator = navigatorCreator();
            var results = IntermediateQueryResult.Empty;
            var stateStore = new FuzzyMatchStateStore(navigator, this.maxEditDistance);

            var characterCount = 0;
            do
            {
                foreach (var state in stateStore.GetNextStateEntries())
                {
                    var wordIndex = state.WordIndex;
                    var bookmark = state.Bookmark;
                    bookmark.Apply();

                    if (state.WordIndex == this.Word.Length)
                    {
                        // Don't allow matches that have consisted entirely of edits
                        if (characterCount > state.EditDistance)
                        {
                            // We're out of search characters for this state.
                            if (navigator.HasExactMatches)
                            {
                                // Calculate the weighting as (L-E)/L where
                                // L = Word length
                                // E = Number of edits
                                // So for a word with no edits, we have a weighting of (L-0)/L = 1
                                // All other weightings will be less than 1, with more edits drawing the weighting towards zero
                                var weighting = (double)(characterCount - state.EditDistance) / characterCount;
                                results = results.Union(navigator.GetExactMatches(weighting));
                            }
                            else
                            {
                                // Assume there could be missing characters at the end
                                AddDeletionBookmarks(navigator, stateStore, state);
                            }
                        }
                    }
                    else
                    {
                        var currentCharacter = this.Word[wordIndex];
                        if (navigator.Process(currentCharacter))
                        {
                            // The character matched successfully, so potentially no edits incurred, just move to the next character
                            stateStore.Add(state.ApplyExactMatch(navigator.CreateBookmark(), currentCharacter));
                        }
                        else
                        {
                            // First skip this character (assume extra character inserted), but don't move the navigator on
                            stateStore.Add(state.ApplyInsertion(currentCharacter));

                            // Also try skipping this character (assume omission) by just moving on in the navigator
                            AddDeletionBookmarks(navigator, stateStore, state);
                        }

                        // Always assume this could be a substituted character
                        AddSubstitutionBookmarks(navigator, stateStore, currentCharacter, state);
                    }
                }

                stateStore.PrepareNextEntries();

                characterCount++;
            }
            while (stateStore.HasEntries);

            return results;
        }

        private static void AddSubstitutionBookmarks(IIndexNavigator navigator, FuzzyMatchStateStore stateStore, char currentCharacter, FuzzyMatchState currentState)
        {
            var bookmark = currentState.Bookmark;

            bookmark.Apply();
            foreach (char c in navigator.EnumerateNextCharacters())
            {
                if (currentCharacter == c)
                {
                    // Don't bother applying this substitution,we'll have already processed it as an exact match.
                    continue;
                }

                bookmark.Apply();
                navigator.Process(c);
                stateStore.Add(currentState.ApplySubstitution(navigator.CreateBookmark(), new SubstitutedCharacters(currentCharacter, c)));
            }
        }

        private static void AddDeletionBookmarks(IIndexNavigator navigator, FuzzyMatchStateStore stateStore, FuzzyMatchState currentState)
        {
            var bookmark = currentState.Bookmark;

            bookmark.Apply();
            foreach (char c in navigator.EnumerateNextCharacters())
            {
                bookmark.Apply();
                navigator.Process(c);
                stateStore.Add(currentState.ApplyDeletion(navigator.CreateBookmark(), c));
            }
        }


        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Word;
        }
    }
}
