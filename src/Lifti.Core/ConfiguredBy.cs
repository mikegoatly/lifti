namespace Lifti
{
    /// <summary>
    /// A base class for classes that can be configured by a set of options
    /// </summary>
    public abstract class ConfiguredBy<T> : IConfiguredBy<T>
    {
        private bool configured;

        /// <inheritdoc />
        public void Configure(T options)
        {
            if (!this.configured)
            {
                this.OnConfiguring(options);
                this.configured = true;
            }
        }

        /// <summary>
        /// Invoked when this instance is being configured.
        /// </summary>
        protected abstract void OnConfiguring(T options);
    }
}
