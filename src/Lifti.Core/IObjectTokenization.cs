using Lifti.Tokenization.Objects;
using System;
using System.Collections.Generic;

namespace Lifti
{
    /// <summary>
    /// Describes the configuration that should be used when indexing
    /// an strongly typed item against an index.
    /// </summary>
    internal interface IObjectTokenization
    {
        /// <summary>
        /// Gets the type of the item this instance represents.
        /// </summary>
        Type ItemType { get; }

        /// <summary>
        /// Gets the configuration for the fields associated to this instance.
        /// </summary>
        IEnumerable<IFieldReader> GetConfiguredFields();
    }
}
