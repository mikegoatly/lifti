using System.Collections.Generic;

namespace Lifti
{
    /// <summary>
    /// Extracted phrases matched for the item with the given key.
    /// </summary>
    public record MatchedPhrases<TKey>(TKey Key, IReadOnlyList<string> Phrases);

    /// <summary>
    /// Extracted phrases matched for the given item.
    /// </summary>
    public record MatchedPhrases<TKey, TItem> : MatchedPhrases<TKey>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MatchedPhrases{TKey, TItem}"/>.
        /// </summary>
        public MatchedPhrases(TItem item, TKey key, IReadOnlyList<string> phrases)
            : base(key, phrases)
        {
            this.Item = item;
        }

        /// <summary>
        /// Gets the item that the matched phrases were returned for.
        /// </summary>
        public TItem Item { get; init; }
    }
}
