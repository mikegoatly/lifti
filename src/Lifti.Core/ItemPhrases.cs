using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    /// <summary>
    /// Extracted phrases for a field within an item.
    /// </summary>
    public record FieldPhrases<TKey>(string FoundIn, IReadOnlyList<string> Phrases)
    {
        /// <summary>
        /// Creates a new instance of <see cref="FieldPhrases{TKey}"/>.
        /// </summary>
        public FieldPhrases(string foundIn, params string[] phrases)
            : this(foundIn, phrases as IReadOnlyList<string>)
        {
        }
    }

    /// <summary>
    /// Extracted phrases matched for the given item.
    /// </summary>
    public record ItemPhrases<TKey>(SearchResult<TKey> SearchResult, IReadOnlyList<FieldPhrases<TKey>> FieldPhrases)
    {
        /// <summary>
        /// Enumerates all the matched phrases found within this item regardless of the field they were found in.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> EnumeratePhrases()
        {
            return this.FieldPhrases.SelectMany(x => x.Phrases);
        }
    }

    /// <summary>
    /// Extracted phrases matched for the given item.
    /// </summary>
    public record ItemPhrases<TKey, TItem> : ItemPhrases<TKey>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ItemPhrases{TKey, TItem}"/>.
        /// </summary>
        public ItemPhrases(TItem item, SearchResult<TKey> SearchResult, IReadOnlyList<FieldPhrases<TKey>> phrases)
            : base(SearchResult, phrases)
        {
            this.Item = item;
        }

        /// <summary>
        /// Gets the item that the matched phrases were returned for.
        /// </summary>
        public TItem Item { get; init; }
    }
}
