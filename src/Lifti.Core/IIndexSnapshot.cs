using Lifti.Querying;
using System;

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

        /// <inheritdoc cref="Metadata"/>
        [Obsolete("Use Metadata property instead")]
        IIndexMetadata Items { get; }

        /// <summary>
        /// Gets the <see cref="IIndexMetadata"/> in the state it was in when the snapshot was taken.
        /// </summary>
        IIndexMetadata Metadata { get; }
    }

    /// <summary>
    /// Implemented by classes that provide a point-in-time, read-only snapshot of an index.
    /// </summary>
    public interface IIndexSnapshot<TKey> : IIndexSnapshot
    {

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        /// <inheritdoc cref="Metadata"/>
        [Obsolete("Use Metadata property instead")]
        IIndexMetadata<TKey> Items { get; }

        /// <summary>
        /// Gets the <see cref="IIndexSnapshot{T}"/> in the state it was in when the snapshot was taken.
        /// </summary>
        IIndexMetadata<TKey> Metadata { get; }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
    }
}