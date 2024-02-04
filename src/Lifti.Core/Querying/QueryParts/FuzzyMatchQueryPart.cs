﻿#define TRACK_MATCH_STATE_TEXT
// Enabling TRACK_MATCH_STATE_TEXT in DEBUG builds will allow you inspect the state of a fuzzy match
// as it is being processed using the MatchText property. This is not available in release builds for performance.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Lifti.Querying.QueryParts
{
    internal readonly struct SubstitutedCharacters
    {
        public SubstitutedCharacters(char expected, char replacedWith)
        {
            this.Expected = expected;
            this.ReplacedWith = replacedWith;
        }

        public char Expected { get; }
        public char ReplacedWith { get; }

        internal bool IsTransposition(SubstitutedCharacters substituted)
        {
            return this.Expected == substituted.ReplacedWith && this.ReplacedWith == substituted.Expected;
        }
    }

    /// <summary>
    /// An <see cref="IQueryPart"/> that matches documents that contain a fuzzy match for the given text.
    /// </summary>
    public sealed class FuzzyMatchQueryPart : WordQueryPart
    {
        private static readonly SharedPool<FuzzyMatchStateStore> fuzzyMatchStateStorePool = new(
            static () => new(), 
            static s => s.Clear(),
            3);

        internal const ushort DefaultMaxEditDistance = 4;
        internal const ushort DefaultMaxSequentialEdits = 1;

        private readonly ushort maxEditDistance;
        private readonly ushort maxSequentialEdits;

        private class FuzzyMatchStateStore
        {
            private ushort maxEditDistance;
            private ushort maxSequentialEdits;
            private readonly DoubleBufferedList<FuzzyMatchState> state = [];

            // It's very likely that we'll encounter the same point in the index multiple times while processing a fuzzy match.
            // There's no point in traversing the same part multiple times for a given point in the search term, so this hashset keeps track of each logical
            // location that has been reached at each index within the search term.
            // Note that because we're reusing bookmarks, we can't use the bookmark itself as the hashcode as it will change as the bookmark is reused,
            // so instead we use the hashcode of the bookmark directly. Although this is not guaranteed to be unique, it's good enough for our purposes and
            // saves thousands of allocations per query.
            private readonly HashSet<(int wordIndex, int bookmarkHash)> processedBookmarks = [];

            public FuzzyMatchStateStore()
            {
            }

            public void Initialize(IIndexNavigator navigator, ushort maxEditDistance, ushort maxSequentialEdits)
            {
                this.state.AddToCurrent(new FuzzyMatchState(navigator.CreateBookmark()));
                this.maxEditDistance = maxEditDistance;
                this.maxSequentialEdits = maxSequentialEdits;
            }

            public bool HasEntries
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return this.state.Count > 0; }
            }

            public bool Add(FuzzyMatchState state, bool disposeIfNotUsed)
            {
                if (state.TotalEditCount <= this.maxEditDistance &&
                    state.SequentialEdits <= this.maxSequentialEdits &&
                    this.processedBookmarks.Add((state.WordIndex, state.Bookmark.GetHashCode())))
                {
                    this.state.Add(state);
                    return true;
                }
                else
                {
                    if (disposeIfNotUsed)
                    {
                        // Dispose of the bookmark as we're not going to use it
                        state.Dispose();
                    }

                    return false;
                }
            }

            public DoubleBufferedList<FuzzyMatchState> GetNextStateEntries()
            {
                return this.state;
            }

            public void PrepareNextEntries()
            {
                this.state.Swap();
            }

            public void Clear()
            {
                this.state.Clear();
                this.processedBookmarks.Clear();
            }
        }

        private readonly struct FuzzyMatchState : IDisposable
        {
            /// <summary>
            /// Creates a new <see cref="FuzzyMatchState"/> instance.
            /// </summary>
            /// <param name="bookmark">The <see cref="IIndexNavigatorBookmark"/> for the state of the index that this instance is for.</param>
            /// <param name="totalEditCount">The current number number of edits this required to reach this point in the match.</param>
            /// <param name="levenshteinDistance">The Levenshtein distance accumulated so far. This will differ from <paramref name="totalEditCount"/> 
            /// only when substitutions are encountered, in which case an extra 1 per substitution will be accumulated here.</param>
            /// <param name="sequentialEdits">The number of sequential edits that have accumulated so far to reach this point. When
            /// an exact match is processed, this will be reset to zero.</param>
            /// <param name="wordIndex">The current index in the search term.</param>
            /// <param name="lastSubstitution">The character substitution that was made at the last character, or null if the last operation was 
            /// not a substitution. This is used to identify the case where letters have been transposed.</param>
            public FuzzyMatchState(
                IIndexNavigatorBookmark bookmark,
                ushort totalEditCount,
                ushort levenshteinDistance,
                ushort sequentialEdits,
                ushort wordIndex,
#if DEBUG && TRACK_MATCH_STATE_TEXT
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
                string matchText,
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
#endif
                SubstitutedCharacters? lastSubstitution = null
                )
            {
                this.Bookmark = bookmark;
                this.TotalEditCount = totalEditCount;
                this.LevenshteinDistance = levenshteinDistance;
                this.SequentialEdits = sequentialEdits;
                this.WordIndex = wordIndex;
                this.LastSubstitution = lastSubstitution;
#if DEBUG && TRACK_MATCH_STATE_TEXT
                this.MatchText = matchText;
#endif
            }

            public FuzzyMatchState(IIndexNavigatorBookmark bookmark)
                : this(
                      bookmark,
                      0,
                      0,
                      0,
                      0
#if DEBUG && TRACK_MATCH_STATE_TEXT
                      , string.Empty
#endif
                      )
            {
            }

            public IIndexNavigatorBookmark Bookmark { get; }

            public ushort TotalEditCount { get; }
            public ushort LevenshteinDistance { get; }

            /// <summary>
            /// The index that this state is currently matching in the target word.
            /// </summary>
            public ushort WordIndex { get; }

            public SubstitutedCharacters? LastSubstitution { get; }
            public ushort SequentialEdits { get; }

#if DEBUG && TRACK_MATCH_STATE_TEXT
            public string MatchText { get; }
#endif

            public FuzzyMatchState ApplySubstitution(IIndexNavigatorBookmark newBookmark, SubstitutedCharacters substituted)
            {
                // Check to see if this substitution is the exact opposite of the previous, to detect character transpositions.
                // In this case we will what would otherwise be two substitution edits as a single edit.
                // We will also not track the substitution at this point to prevent incorrect transpositions being
                // inferred at the next character.
                var isTransposition = this.LastSubstitution.GetValueOrDefault().IsTransposition(substituted);

                return new FuzzyMatchState(
                    newBookmark,
                    isTransposition ? this.TotalEditCount : (ushort)(this.TotalEditCount + 1),
                    isTransposition ? this.LevenshteinDistance : (ushort)(this.LevenshteinDistance + 2),
                    isTransposition ? this.SequentialEdits : (ushort)(this.SequentialEdits + 1),
                    (ushort)(this.WordIndex + 1),
#if DEBUG && TRACK_MATCH_STATE_TEXT
                    this.MatchText + $"(-{substituted.Expected}+{substituted.ReplacedWith})",
#endif
                    isTransposition ? null : substituted);
            }

            public FuzzyMatchState ApplyDeletion(
                IIndexNavigatorBookmark newBookmark
#if DEBUG && TRACK_MATCH_STATE_TEXT
                , char omittedCharacter
#endif
                )
            {
                return new FuzzyMatchState(
                    newBookmark,
                    (ushort)(this.TotalEditCount + 1),
                    (ushort)(this.LevenshteinDistance + 1),
                    (ushort)(this.SequentialEdits + 1),
                    this.WordIndex
#if DEBUG && TRACK_MATCH_STATE_TEXT
                    , this.MatchText + $"(-{omittedCharacter})"
#endif
                    );
            }

            public FuzzyMatchState ApplyInsertion(
#if DEBUG && TRACK_MATCH_STATE_TEXT
                char additionalCharacter
#endif
                )
            {
                return new FuzzyMatchState(
                    this.Bookmark,
                    (ushort)(this.TotalEditCount + 1),
                    (ushort)(this.LevenshteinDistance + 1),
                    (ushort)(this.SequentialEdits + 1),
                    (ushort)(this.WordIndex + 1)
#if DEBUG && TRACK_MATCH_STATE_TEXT
                    , this.MatchText + $"(+{additionalCharacter})"
#endif
                    );
            }

            public FuzzyMatchState ApplyExactMatch(
                IIndexNavigatorBookmark nextBookmark
#if DEBUG && TRACK_MATCH_STATE_TEXT
                , char currentCharacter
#endif
                )
            {
                return new FuzzyMatchState(
                    nextBookmark,
                    this.TotalEditCount,
                    this.LevenshteinDistance,
                    0, // Reset sequential edits when an exact match occurs
                    (ushort)(this.WordIndex + 1)
#if DEBUG && TRACK_MATCH_STATE_TEXT
                    , this.MatchText + currentCharacter
#endif
                    );
            }

            public void Dispose()
            {
                this.Bookmark.Dispose();
            }
        }

        /// <summary>
        /// Constructs a new instance of <see cref="FuzzyMatchQueryPart"/>.
        /// </summary>
        /// <param name="word">The word to search for in the index.</param>
        /// <param name="maxEditDistance">The maximum of edits allowed for any given match. The higher this value, the more divergent 
        /// matches will be.</param>
        /// <param name="maxSequentialEdits">The maximum number of edits that are allowed to appear sequentially. By default this is 1,
        /// which forces matches to be more similar to the search criteria.</param>
        /// <param name="scoreBoost">
        /// The score boost to apply to any matches of the search term. This is multiplied with any score boosts
        /// applied to matching fields. A null value indicates that no additional score boost should be applied.
        /// </param>
        /// <remarks>
        /// A transposition of neighbouring characters is considered as single edit, not two distinct substitutions.
        /// </remarks>
        public FuzzyMatchQueryPart(string word, ushort maxEditDistance = DefaultMaxEditDistance, ushort maxSequentialEdits = DefaultMaxSequentialEdits, double? scoreBoost = null)
            : base(word, scoreBoost)
        {
            this.maxEditDistance = maxEditDistance;
            this.maxSequentialEdits = maxSequentialEdits;
        }

        /// <inheritdoc/>
        protected override double RunWeightingCalculation(Func<IIndexNavigator> navigatorCreator)
        {
            return base.RunWeightingCalculation(navigatorCreator)
                + this.maxEditDistance
                + ((this.maxSequentialEdits - 1) << 1);
        }

        /// <inheritdoc/>
        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext)
        {
            if (navigatorCreator == null)
            {
                throw new ArgumentNullException(nameof(navigatorCreator));
            }

            if (queryContext is null)
            {
                throw new ArgumentNullException(nameof(queryContext));
            }

            var timing = queryContext.ExecutionTimings.Start(this, queryContext);
            using var navigator = navigatorCreator();
            var resultCollector = new DocumentMatchCollector();
            var stateStore = fuzzyMatchStateStorePool.Take();
            stateStore.Initialize(navigator, this.maxEditDistance, this.maxSequentialEdits);

            var characterCount = 0;
            var searchTermLength = this.Word.Length;
            var scoreBoost = this.ScoreBoost ?? 1D;

            do
            {
                foreach (var state in stateStore.GetNextStateEntries())
                {
                    var wordIndex = state.WordIndex;
                    state.Bookmark.Apply();
                    var disposeBookmark = true;

                    if (state.WordIndex == searchTermLength)
                    {
                        // Don't allow matches that have consisted entirely of edits
                        if (characterCount > state.TotalEditCount)
                        {
                            // We're out of search characters for this state.
                            if (navigator.HasExactMatches)
                            {
                                // Use Levenshtein Distance to calculate the weighting
                                // Calculate the weighting as ((L1 + L2)-E)/(L1 + L2) where
                                // L1 = Search term length
                                // L2 = Matched term length
                                // E = The Levenshtein distance between the search term and match
                                // So for a word with no edits, we have a weighting of (L-0)/L = 1
                                // All other weightings will be less than 1, with more edits drawing the weighting towards zero
                                var lengthTotal = searchTermLength + characterCount;
                                var weighting = (double)(lengthTotal - state.LevenshteinDistance) / lengthTotal;
                                weighting *= scoreBoost;

                                navigator.AddExactMatches(queryContext, resultCollector, weighting);
                            }

                            // Always assume there could be missing characters at the end
                            AddDeletionBookmarks(navigator, stateStore, state);
                        }
                    }
                    else
                    {
                        var currentCharacter = this.Word[wordIndex];
                        if (navigator.Process(currentCharacter))
                        {
                            // The character matched successfully, so potentially no edits incurred, just move to the next character
                            stateStore.Add(state.ApplyExactMatch(
                                navigator.CreateBookmark()
#if DEBUG && TRACK_MATCH_STATE_TEXT
                                , currentCharacter
#endif
                                ), true);
                        }
                        else
                        {
                            // First skip this character (assume extra character inserted), but don't move the navigator on
                            // We'll handle this case specially if the state wasn't added as this will allow us to dispose of the
                            // current bookmark after it has been used to add new deletion bookmarks
                            disposeBookmark = !stateStore.Add(state.ApplyInsertion(
#if DEBUG && TRACK_MATCH_STATE_TEXT
                                currentCharacter
#endif
                                ), false);

                            // Also try skipping this character (assume omission) by just moving on in the navigator
                            AddDeletionBookmarks(navigator, stateStore, state);
                        }

                        // Always assume this could be a substituted character
                        AddSubstitutionBookmarks(navigator, stateStore, currentCharacter, state);
                    }

                    // We're done with this entry now. Disposing it causes the bookmark to get disposed and returned
                    // to the pool in the index navigator for reuse.
                    if (disposeBookmark)
                    {
                        state.Dispose();
                    }
                }

                stateStore.PrepareNextEntries();

                characterCount++;
            }
            while (stateStore.HasEntries);

            fuzzyMatchStateStorePool.Return(stateStore);

            return timing.Complete(resultCollector.ToIntermediateQueryResult());
        }

        private static void AddSubstitutionBookmarks(IIndexNavigator navigator, FuzzyMatchStateStore stateStore, char currentCharacter, FuzzyMatchState currentState)
        {
            var bookmark = currentState.Bookmark;

            bookmark.Apply();
            foreach (var c in navigator.EnumerateNextCharacters())
            {
                if (currentCharacter == c)
                {
                    // Don't bother applying this substitution,we'll have already processed it as an exact match.
                    continue;
                }

                bookmark.Apply();
                navigator.Process(c);
                stateStore.Add(
                    currentState.ApplySubstitution(
                        navigator.CreateBookmark(), 
                        new SubstitutedCharacters(currentCharacter, c)),
                    true);
            }
        }

        private static void AddDeletionBookmarks(IIndexNavigator navigator, FuzzyMatchStateStore stateStore, FuzzyMatchState currentState)
        {
            var bookmark = currentState.Bookmark;

            bookmark.Apply();
            foreach (var c in navigator.EnumerateNextCharacters())
            {
                bookmark.Apply();
                navigator.Process(c);
                stateStore.Add(
                    currentState.ApplyDeletion(
                        navigator.CreateBookmark()
#if DEBUG && TRACK_MATCH_STATE_TEXT
                        , c
#endif
                    ),
                    true);
            }
        }


        /// <inheritdoc/>
        public override string ToString()
        {
            string searchTerm;
            if (this.maxEditDistance != DefaultMaxEditDistance || this.maxSequentialEdits != DefaultMaxSequentialEdits)
            {
                searchTerm = $"?{(this.maxEditDistance != DefaultMaxEditDistance ? this.maxEditDistance : "")},{(this.maxSequentialEdits != DefaultMaxSequentialEdits ? this.maxSequentialEdits : "")}?{this.Word}";
            }
            else
            {
                searchTerm = "?" + this.Word;
            }

            return base.ToString(searchTerm);
        }
    }
}
