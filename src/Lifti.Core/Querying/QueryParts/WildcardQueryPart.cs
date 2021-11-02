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
                var resultFragments = new List<IntermediateQueryResult>();
                var bookmarkStack = new Stack<IIndexNavigatorBookmark>();
                var nextBookmarks = new List<IIndexNavigatorBookmark>();
                bookmarkStack.Push(navigator.CreateBookmark());

                for (var i = 0; i < fragments.Count; i++)
                {
                    var nextFragment = i == fragments.Count - 1 ? (WildcardQueryFragment?)null : fragments[i + 1];

                    do
                    {
                        bookmarkStack.Pop().RewindNavigator();
                        foreach (var bookmark in ProcessFragment(
                            navigator,
                            resultFragments,
                            fragments[i],
                            nextFragment))
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

                if (resultFragments.Count == 0)
                {
                    return IntermediateQueryResult.Empty;
                }
                else
                {
                    return queryContext.ApplyTo(new IntermediateQueryResult(resultFragments));
                }
            }
        }

        private static IEnumerable<IIndexNavigatorBookmark> ProcessFragment(
            IIndexNavigator navigator,
            IList<IntermediateQueryResult> fragmentResults,
            WildcardQueryFragment fragment,
            WildcardQueryFragment? nextFragment)
        {
            switch (fragment.Kind)
            {
                case WildcardQueryFragmentKind.Text:
                    if (!navigator.Process(fragment.Text.AsSpan()))
                    {
                        // No matches - nothing can return and no subsequent fragments can change that
                        return Array.Empty<IIndexNavigatorBookmark>();
                    }

                    if (nextFragment == null)
                    {
                        // This is the end of the query and we've ended up on some exact matches
                        fragmentResults.Add(navigator.GetExactMatches());
                        return Array.Empty<IIndexNavigatorBookmark>();
                    }

                    // Return a bookmark at the current location - we will continue from this point on the next iteration
                    return new[] { navigator.CreateBookmark() };

                case WildcardQueryFragmentKind.MultiCharacter:
                    if (nextFragment == null)
                    {
                        // This wildcard is the last in the pattern - just return any exact and child matches under the current position
                        // I.e. as per the classic "starts with" operator
                        fragmentResults.Add(navigator.GetExactAndChildMatches());

                        // No other work to process - no more bookmarks required.
                        return Array.Empty<IIndexNavigatorBookmark>();
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
                    return CreateBookmarksForAllChildCharacters(navigator);

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
            throw new NotImplementedException();
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
