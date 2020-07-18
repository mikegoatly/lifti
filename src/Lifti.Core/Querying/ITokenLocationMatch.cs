using System.Collections.Generic;

namespace Lifti.Querying
{
    /// <summary>
    /// Describes information about a token matched whilst executing a query.
    /// </summary>
    public interface ITokenLocationMatch
    {
        /// <summary>
        /// Gets the maximum index in the field that the token matched.
        /// </summary>
        int MaxTokenIndex { get; }

        /// <summary>
        /// Gets the minimum index in the field that the token matched.
        /// </summary>
        int MinTokenIndex { get; }

        /// <summary>
        /// Gets all the <see cref="TokenLocation"/>s at which this token matched.
        /// </summary>
        /// <returns></returns>
        IEnumerable<TokenLocation> GetLocations();
    }
}
