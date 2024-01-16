using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// An <see cref="IQueryPart"/> that matches text in a document using the following wildcard rules:
    /// "*" matches any number of characters (<see cref="WildcardQueryFragmentKind.MultiCharacter"/>)
    /// "%" matches a single character (<see cref="WildcardQueryFragmentKind.SingleCharacter"/>)
    /// </summary>
    public sealed class WildcardQueryPart : ScoreBoostedQueryPart
    {
        private static readonly IIndexNavigatorBookmark[] QueryCompleted = [];
        internal IReadOnlyList<WildcardQueryFragment> Fragments { get; }

        /// <summary>
        /// Creates a new instance of <see cref="WildcardQueryPart"/>.
        /// </summary>
        public WildcardQueryPart(params WildcardQueryFragment[] fragments)
            : this((IEnumerable<WildcardQueryFragment>)fragments)
        {

        }

        /// <summary>
        /// Creates a new instance of <see cref="WildcardQueryPart"/>.
        /// </summary>
        public WildcardQueryPart(IEnumerable<WildcardQueryFragment> fragments, double? scoreBoost = null)
            : base(scoreBoost)
        {
            if (fragments is null)
            {
                throw new ArgumentNullException(nameof(fragments));
            }

            this.Fragments = NormalizeFragmentSequence(fragments).ToList();

            if (this.Fragments.Count == 0)
            {
                throw new QueryParserException(ExceptionMessages.EmptyWildcardQuery);
            }
        }

        /// <inheritdoc />
        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext)
        {
            if (navigatorCreator is null)
            {
                throw new ArgumentNullException(nameof(navigatorCreator));
            }

            if (queryContext is null)
            {
                throw new ArgumentNullException(nameof(queryContext));
            }

            using var navigator = navigatorCreator();
            var resultCollector = new DocumentMatchCollector();
            var bookmarks = new DoubleBufferedList<IIndexNavigatorBookmark>(navigator.CreateBookmark());
            var scoreBoost = this.ScoreBoost ?? 1D;
            for (var i = 0; i < this.Fragments.Count && bookmarks.Count > 0; i++)
            {
                var nextFragment = i == this.Fragments.Count - 1 ? (WildcardQueryFragment?)null : this.Fragments[i + 1];

                foreach (var bookmark in bookmarks)
                {
                    bookmark.Apply();

                    var nextBookmarks = ProcessFragment(
                        navigator,
                        resultCollector,
                        scoreBoost,
                        queryContext,
                        this.Fragments[i],
                        nextFragment);

                    bookmarks.AddRange(nextBookmarks);

                    bookmark.Dispose();
                }

                bookmarks.Swap();
            }

            return resultCollector.ToIntermediateQueryResult();
        }

        /// <inheritdoc />
        protected override double RunWeightingCalculation(Func<IIndexNavigator> navigatorCreator)
        {
            var firstFragment = this.Fragments[0];
            if (this.Fragments.Count == 1 && firstFragment.Kind == WildcardQueryFragmentKind.MultiCharacter)
            {
                // Penalise the use of a full document search
                return 1000D;
            }

            int weight = 0;

            for (var i = 0; i < this.Fragments.Count; i++)
            {
                switch (this.Fragments[i].Kind)
                {
                    case WildcardQueryFragmentKind.MultiCharacter:
                        weight += 4;
                        break;
                    case WildcardQueryFragmentKind.SingleCharacter:
                        weight += 1;
                        break;
                    case WildcardQueryFragmentKind.Text:
                        weight += 1;
                        break;

                }
            }

            return firstFragment.Kind switch
            {
                // Don't penalise wildcard searches that start with a text fragment as badly
                WildcardQueryFragmentKind.Text => weight * 0.8D,
                // Penalise the use of leading multi-character wildcards
                WildcardQueryFragmentKind.MultiCharacter => weight * 1.2D,
                _ => weight,
            };
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var fragment in this.Fragments)
            {

                builder.Append(fragment.Kind switch
                {
                    WildcardQueryFragmentKind.Text => fragment.Text,
                    WildcardQueryFragmentKind.MultiCharacter => "*",
                    WildcardQueryFragmentKind.SingleCharacter => "%",
                    _ => "?"
                });
            }

            return base.ToString(builder.ToString());
        }

        private static IEnumerable<IIndexNavigatorBookmark> ProcessFragment(
            IIndexNavigator navigator,
            DocumentMatchCollector resultCollector,
            double scoreBoost,
            QueryContext queryContext,
            WildcardQueryFragment fragment,
            WildcardQueryFragment? nextFragment)
        {
            switch (fragment.Kind)
            {
                case WildcardQueryFragmentKind.Text:
                    if (!navigator.Process(fragment.Text.AsSpan()))
                    {
                        // No matches - nothing can return and no subsequent fragments can change that
                        return QueryCompleted;
                    }

                    if (nextFragment == null)
                    {
                        // This is the end of the query and we've ended up on some exact matches
                        navigator.AddExactMatches(queryContext, resultCollector, scoreBoost);
                        return QueryCompleted;
                    }

                    // Return a bookmark at the current location - we will continue from this point on the next iteration
                    return new[] { navigator.CreateBookmark() };

                case WildcardQueryFragmentKind.MultiCharacter:
                    if (nextFragment == null)
                    {
                        // This wildcard is the last in the pattern - just return any exact and child matches under the current position
                        // I.e. as per the classic "starts with" operator
                        navigator.AddExactAndChildMatches(queryContext, resultCollector, scoreBoost);

                        // No other work to process - no more bookmarks required.
                        return QueryCompleted;
                    }
                    else
                    {
                        var nextFragmentValue = nextFragment.Value;
                        if (nextFragmentValue.Kind != WildcardQueryFragmentKind.Text)
                        {
                            throw new LiftiException(ExceptionMessages.ExpectedTextQueryFragmentAfterMultiCharacterWildcard);
                        }

                        if (nextFragmentValue.Text?.Length > 0)
                        {
                            var terminatingCharacter = nextFragmentValue.Text![0];

                            return RecursivelyCreateBookmarksAtMatchingCharacter(navigator, terminatingCharacter);
                        }
                        else
                        {
                            throw new LiftiException(ExceptionMessages.EmptyOrMissingTextQueryFragmentValue);
                        }
                    }

                case WildcardQueryFragmentKind.SingleCharacter:
                    if (nextFragment == null)
                    {
                        // Add all exact matches for every character under the current position
                        using var bookmark = navigator.CreateBookmark();
                        foreach (var character in navigator.EnumerateNextCharacters())
                        {
                            navigator.Process(character);
                            navigator.AddExactMatches(queryContext, resultCollector, scoreBoost);
                            bookmark.Apply();
                        }

                        return QueryCompleted;
                    }
                    else
                    {
                        return CreateBookmarksForAllChildCharacters(navigator);
                    }

                default:
                    throw new ArgumentException("Unknown WildcardQueryFragmentKind: " + fragment.Kind);
            }
        }

        private static IEnumerable<IIndexNavigatorBookmark> CreateBookmarksForAllChildCharacters(IIndexNavigator navigator)
        {
            using var bookmark = navigator.CreateBookmark();
            foreach (var character in navigator.EnumerateNextCharacters())
            {
                navigator.Process(character);
                yield return navigator.CreateBookmark();
                bookmark.Apply();
            }
        }

        private static IEnumerable<IIndexNavigatorBookmark> RecursivelyCreateBookmarksAtMatchingCharacter(IIndexNavigator navigator, char terminatingCharacter)
        {
            var bookmarkStack = new Stack<IIndexNavigatorBookmark>();
            bookmarkStack.Push(navigator.CreateBookmark());

            while (bookmarkStack.Count > 0)
            {
                using var bookmark = bookmarkStack.Pop();
                bookmark.Apply();

                foreach (var character in navigator.EnumerateNextCharacters())
                {
                    if (character == terminatingCharacter)
                    {
                        // This node has a child node that matches the terminating character - return a bookmark at this point
                        // so the next text fragment can be processed from here
                        yield return navigator.CreateBookmark();
                    }

                    // Even if the character matches the terminating character, we still need to process it, as it may not be
                    // the start of a successfully matched sequence. E.g. if the query is "*ERS" and we're in a hierarchy for the token "CENTERS"
                    // the "E" would be the terminating character, but we need to keep going to find the second "E" for "ERS" to match.
                    // Process the character from this node
                    navigator.Process(character);

                    // Push a bookmark so we can return to this point after processing all the characters from this node
                    bookmarkStack.Push(navigator.CreateBookmark());

                    // Return to the node we just processed the character from
                    bookmark.Apply();
                }
            }
        }

        private static IEnumerable<WildcardQueryFragment> NormalizeFragmentSequence(IEnumerable<WildcardQueryFragment> fragments)
        {
            var collapseWildcards = false;
            foreach (var fragment in fragments)
            {
                if (fragment.Kind == WildcardQueryFragmentKind.Text)
                {
                    collapseWildcards = false;
                    yield return fragment;
                }
                else
                {
                    if (collapseWildcards == false)
                    {
                        if (fragment.Kind == WildcardQueryFragmentKind.MultiCharacter)
                        {
                            collapseWildcards = true;
                        }

                        yield return fragment;
                    }
                    else if (fragment.Kind == WildcardQueryFragmentKind.SingleCharacter)
                    {
                        throw new QueryParserException(ExceptionMessages.SingleCharacterWildcardsFollowingMultiCharacterNotSupported);
                    }
                }
            }
        }
    }
}
