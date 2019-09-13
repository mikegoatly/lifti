namespace Lifti
{
    public class FullTextIndexOptions
    {
        public TokenizationOptions TokenizationOptions { get; } = new TokenizationOptions();
        public AdvancedOptions Advanced { get; } = new AdvancedOptions();
    }

    public class FullTextIndexOptions<TKey> : FullTextIndexOptions
    {
    }

    public class AdvancedOptions
    {
        public int SupportIntraNodeTextAfterCharacterIndex { get; set; }
    }
}
