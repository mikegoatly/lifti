using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// An <see cref="IQueryPart"/> that matches items following wildcard rules:
    /// "*" matches any number of characters
    /// "%" matches a single character
    /// </summary>
    public class WildcardQueryPart : IQueryPart
    {
        private static readonly IIndexNavigatorBookmark[] QueryCompleted = Array.Empty<IIndexNavigatorBookmark>();

        private readonly IReadOnlyList<WildcardQueryFragment> fragments;

        /// <summary>
        /// Creates a new instance of <see cref="WildcardQueryPart"/>.
        /// </summary>
        public WildcardQueryPart(IEnumerable<WildcardQueryFragment> fragments)
        {
            if (fragments is null)
            {
                throw new ArgumentNullException(nameof(fragments));
            }

            this.fragments = NormalizeFragmentSequence(fragments).ToList();
        }

        /// <inheritdoc />
        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, IQueryContext queryContext)
        {
            if (navigatorCreator is null)
            {
                throw new ArgumentNullException(nameof(navigatorCreator));
            }

            using (var navigator = navigatorCreator())
            {
                var bookmarkStack = new Stack<IIndexNavigatorBookmark>();
                var nextBookmarks = new List<IIndexNavigatorBookmark>();
                var results = IntermediateQueryResult.Empty;
                bookmarkStack.Push(navigator.CreateBookmark());

                for (var i = 0; i < fragments.Count && bookmarkStack.Count > 0; i++)
                {
                    nextBookmarks.Clear();
                    var nextFragment = i == fragments.Count - 1 ? (WildcardQueryFragment?)null : fragments[i + 1];

                    do
                    {
                        bookmarkStack.Pop().RewindNavigator();
                        foreach (var bookmark in ProcessFragment(
                            navigator,
                            fragments[i],
                            nextFragment,
                            ref results))
                        {
                            nextBookmarks.Add(bookmark);
                        }
                    }
                    while (bookmarkStack.Count > 0);

                    foreach (var bookmark in nextBookmarks)
                    {
                        bookmarkStack.Push(bookmark);
                    }
                }

                return results;
            }
        }

        private static IEnumerable<IIndexNavigatorBookmark> ProcessFragment(
            IIndexNavigator navigator,
            WildcardQueryFragment fragment,
            WildcardQueryFragment? nextFragment,
            ref IntermediateQueryResult results)
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
                        results = results.Union(navigator.GetExactMatches());
                        return QueryCompleted;
                    }

                    // Return a bookmark at the current location - we will continue from this point on the next iteration
                    return new[] { navigator.CreateBookmark() };

                case WildcardQueryFragmentKind.MultiCharacter:
                    if (nextFragment == null)
                    {
                        // This wildcard is the last in the pattern - just return any exact and child matches under the current position
                        // I.e. as per the classic "starts with" operator
                        results = results.Union(navigator.GetExactAndChildMatches());

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
                        var bookmark = navigator.CreateBookmark();
                        foreach (var character in navigator.EnumerateNextCharacters())
                        {
                            navigator.Process(character);
                            results = results.Union(navigator.GetExactMatches());
                            bookmark.RewindNavigator();
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
            var bookmark = navigator.CreateBookmark();
            foreach (var character in navigator.EnumerateNextCharacters())
            {
                navigator.Process(character);
                yield return navigator.CreateBookmark();
                bookmark.RewindNavigator();
            }
        }

        private static IEnumerable<IIndexNavigatorBookmark> RecursivelyCreateBookmarksAtMatchingCharacter(IIndexNavigator navigator, char terminatingCharacter)
        {
            var bookmark = navigator.CreateBookmark();
            foreach (var character in navigator.EnumerateNextCharacters())
            {
                if (character == terminatingCharacter)
                {
                    yield return navigator.CreateBookmark();
                }

                navigator.Process(character);

                foreach (var recursedBookmark in RecursivelyCreateBookmarksAtMatchingCharacter(navigator, terminatingCharacter))
                {
                    yield return recursedBookmark;
                }

                bookmark.RewindNavigator();
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
                }
            }
        }
    }
}
