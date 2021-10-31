namespace Lifti.Querying
{
    /// <summary>
    /// A bookmark for the state of an <see cref="IIndexNavigator"/>, allowing for subsequent query state
    /// to be rolled back to a certain point in time.
    /// </summary>
    public interface IIndexNavigatorBookmark
    {
        /// <summary>
        /// Rewinds the <see cref="IIndexNavigator"/> that this bookmark was created for to the navigation
        /// state it was in when this instance was created.
        /// </summary>
        void RewindNavigator();
    }
}