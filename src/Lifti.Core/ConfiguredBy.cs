namespace Lifti
{
    public abstract class ConfiguredBy<T> : IConfiguredBy<T>
    {
        private bool configured;

        public void Configure(T options)
        {
            if (!this.configured)
            {
                this.OnConfiguring(options);
                this.configured = true;
            }
        }

        protected abstract void OnConfiguring(T options);
    }
}
