using System;

namespace Lifti
{
    /// <summary>
    /// Describes the configuration that should be used when indexing
    /// a strongly typed item against an index.
    /// </summary>
    internal interface IIndexedObjectConfiguration
    {
        /// <summary>
        /// Gets the type of the item this instance represents.
        /// </summary>
        Type ItemType { get; }
    }
}
