using System;
using System.Collections.Generic;

namespace Lifti
{
    internal interface ITokenLocation : IComparable<ITokenLocation>, IEquatable<ITokenLocation>
    {
        /// <summary>
        /// Gets the maximum index in the field that the token matched.
        /// </summary>
        int MaxTokenIndex { get; }

        /// <summary>
        /// Gets the minimum index in the field that the token matched.
        /// </summary>
        int MinTokenIndex { get; }

        void AddTo(HashSet<TokenLocation> collector);
    }
}
