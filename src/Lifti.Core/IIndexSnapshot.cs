using Lifti.Querying;

namespace Lifti
{
    /// <summary>
    /// Implemented by classes that provide a point-in-time, read-only snapshot of an index.
    /// </summary>
    public interface IIndexSnapshot
    {
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

        /// <summary>
        /// Gets the <see cref="IItemStore"/> in the state it was in when the snapshot was taken.
        /// </summary>
        IItemStore Items { get; }
    }

    /// <summary>
    /// Implemented by classes that provide a point-in-time, read-only snapshot of an index.
    /// </summary>
    public interface IIndexSnapshot<TKey> : IIndexSnapshot
    {
        /// <summary>
        /// Gets the <see cref="IItemStore{T}"/> in the state it was in when the snapshot was taken.
        /// </summary>
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        IItemStore<TKey> Items { get; }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
    }
}