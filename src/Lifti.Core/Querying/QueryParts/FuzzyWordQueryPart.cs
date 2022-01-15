#define TRACK_MATCH_STATE_TEXT
// Enabling TRACK_MATCH_STATE_TEXT in DEBUG builds will allow you inspect the state of a fuzzy match
// as it is being processed using the MatchText property. This is not available in release builds for performance.

using System;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// An <see cref="IQueryPart"/> that matches items that contain an fuzzy match for the given text.
    /// </summary>
    public class FuzzyWordQueryPart : WordQueryPart
    {
        private readonly int maxEditDistance;

        private enum EditKind
        {
            Substitution,
            Deletion
        }

        private struct FuzzyMatchState
        {
#if DEBUG && TRACK_MATCH_STATE_TEXT
            public FuzzyMatchState(
                IIndexNavigatorBookmark bookmark,
                int editCount,
                int wordIndex,
                string matchText) : this()
            {
                this.Bookmark = bookmark;
                this.EditDistance = editCount;
                this.WordIndex = wordIndex;
                this.MatchText = matchText;
            }
#else
            public FuzzyMatchState(IIndexNavigatorBookmark bookmark, int editDistance, int wordIndex) : this()
            {
                this.Bookmark = bookmark;
                this.EditDistance = editDistance;
                this.WordIndex = wordIndex;
            }
#endif

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
        }

        /// <summary>
        /// Constructs a new instance of <see cref="ExactWordQueryPart"/>.
        /// </summary>
        public FuzzyWordQueryPart(string word, int maxEditDistance)
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
            var stateStore = new DoubleBufferedList<FuzzyMatchState>(new FuzzyMatchState(navigator.CreateBookmark()));

            var characterCount = 0;
            do
            {
                foreach (var state in stateStore)
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
                                results = results.Union(navigator.GetExactMatches());
                            }
                            else
                            {
                                // Assume there could be missing characters at the end
                                AddBookmarksForAllTraversableCharacters(navigator, stateStore, state, EditKind.Deletion);
                            }
                        }
                    }
                    else
                    {
                        var currentCharacter = this.Word[wordIndex];
                        void AddToStateStore(IIndexNavigatorBookmark bookmark, int editDistance)
                        {
                            stateStore.Add(
                                new FuzzyMatchState(
                                    bookmark,
                                    editDistance,
                                    wordIndex + 1
#if DEBUG && TRACK_MATCH_STATE_TEXT
                                , state.MatchText + currentCharacter
#endif
                                ));
                        }

                        if (navigator.Process(currentCharacter))
                        {
                            // The character matched successfully, so potentially no edits incurred, just move to the next character
                            AddToStateStore(navigator.CreateBookmark(), state.EditDistance);
                        }
                        else
                        {
                            // First skip this character (assume extra character inserted), but don't move the navigator on
                            if (state.EditDistance < this.maxEditDistance)
                            {
                                AddToStateStore(bookmark, state.EditDistance + 1);
                            }

                            // Also try skipping this character (assume omission) by just moving on in the navigator
                            AddBookmarksForAllTraversableCharacters(navigator, stateStore, state, EditKind.Deletion);
                        }

                        // Always assume this could be a substituted character
                        AddBookmarksForAllTraversableCharacters(navigator, stateStore, state, EditKind.Substitution, except: currentCharacter);
                    }
                }

                stateStore.Swap();

                characterCount++;
            }
            while (stateStore.Count > 0);

            // TODO adjust score based on edit count
            return results;
        }

        private void AddBookmarksForAllTraversableCharacters(
            IIndexNavigator navigator,
            DoubleBufferedList<FuzzyMatchState> stateStore,
            FuzzyMatchState currentState,
            EditKind editKind,
            char? except = null)
        {
            var bookmark = currentState.Bookmark;
            var nextEditDistance = currentState.EditDistance + 1;
            var nextIndex = editKind switch
            {
                EditKind.Deletion => currentState.WordIndex,
                _ => currentState.WordIndex + 1
            };

            if (nextEditDistance > this.maxEditDistance)
            {
                // Stop processing - we've reached the maximum number of allowed edits
                return;
            }

            bookmark.Apply();
            foreach (char c in navigator.EnumerateNextCharacters())
            {
                if (except == c)
                {
                    continue;
                }

                bookmark.Apply();
                navigator.Process(c);
                stateStore.Add(
                    new FuzzyMatchState(
                        navigator.CreateBookmark(),
                        nextEditDistance,
                        nextIndex
#if DEBUG && TRACK_MATCH_STATE_TEXT
                        , editKind switch
                        {
                            EditKind.Substitution => currentState.MatchText + $"(-{this.Word[currentState.WordIndex]}{c}?)",
                            EditKind.Deletion => currentState.MatchText + $"(-{c})",
                            _ => throw new InvalidOperationException("Unsupported edit kind!")
                        }
#endif
                        ));
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Word;
        }
    }
}
