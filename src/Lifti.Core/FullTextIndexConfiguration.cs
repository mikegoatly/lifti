namespace Lifti
{
    public class FullTextIndexConfiguration
    {
        public AdvancedOptions Advanced { get; } = new AdvancedOptions();
    }

    public class FullTextIndexConfiguration<TKey> : FullTextIndexConfiguration
    {
    }
}
