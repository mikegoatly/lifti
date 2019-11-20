using Lifti.Querying;

namespace Lifti
{
    public interface IIndexSnapshot<TKey>
    {
        /// <summary>
        /// Gets the ID lookup in the state it was in when the snapshot was taken.
        /// </summary>
        IIdLookup<TKey> IdLookup { get; }

        /// <summary>
        /// Gets the root node of the index at the time the snapshot was taken.
        /// </summary>
        IndexNode Root { get; }

        /// <summary>
        /// Gets the lookup for the index's configured fields.
        /// </summary>
        IIndexedFieldLookup FieldLookup { get; }

        /// <summary>
        /// Creates an implementation of <see cref="IIndexNavigator"/> that can be used to navigate through the index
        /// on a character by character basis.
        /// </summary>
        IIndexNavigator CreateNavigator();
    }
}