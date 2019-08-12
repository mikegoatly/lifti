namespace Lifti
{
    public class FullTextIndexOptions
    {
        public TokenizationOptions TokenizationOptions { get; set; } = new TokenizationOptions();
    }

    public class FullTextIndexOptions<TKey> : FullTextIndexOptions
    {
    }
}
