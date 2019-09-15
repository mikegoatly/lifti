namespace Lifti
{
    public class FullTextIndexConfiguration
    {
        public AdvancedOptions Advanced { get; } = new AdvancedOptions();
    }

    public class FullTextIndexOptions<TKey> : FullTextIndexConfiguration
    {
    }

    public class AdvancedOptions
    {
        public int SupportIntraNodeTextAfterCharacterIndex { get; set; } = 4;
    }
}
