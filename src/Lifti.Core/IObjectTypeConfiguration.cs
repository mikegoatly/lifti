using Lifti.Tokenization.Objects;
using System;

namespace Lifti
{
    /// <summary>
    /// Describes the configuration that should be used when indexing text from an object type.
    /// </summary>
    internal interface IObjectTypeConfiguration
    {
        /// <summary>
        /// Gets the type of the object this configuration is for.
        /// </summary>
        Type ObjectType { get; }

        /// <summary>
        /// Gets the unique id for the object type.
        /// </summary>
        byte Id { get; }

        /// <summary>
        /// Gets the non-type specific score boost options.
        /// </summary>
        ObjectScoreBoostOptions ScoreBoostOptions { get; }
    }
}
