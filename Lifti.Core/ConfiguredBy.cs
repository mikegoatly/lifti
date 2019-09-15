namespace Lifti
{
    public abstract class ConfiguredBy<T> : IConfiguredBy<T>
    {
        private bool configured;

        void IConfiguredBy<T>.Configure(T options)
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
