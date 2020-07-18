namespace Lifti
{
    /// <summary>
    /// Implemented by classes that can be configured by options.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConfiguredBy<T>
    {
        /// <summary>
        /// Configures this instance from the given options.
        /// </summary>
        void Configure(T options);
    }
}
