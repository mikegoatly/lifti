using System;

namespace Lifti.Querying
{
    /// <summary>
    /// A bookmark for the state of an <see cref="IIndexNavigator"/>, allowing for subsequent query state
    /// to be reset to a certain point in time.
    /// </summary>
    public interface IIndexNavigatorBookmark : IDisposable
    {
        /// <summary>
        /// Resets the <see cref="IIndexNavigator"/> that this bookmark was created for to the navigation
        /// state it was in when this instance was created.
        /// </summary>
        void Apply();
    }
}