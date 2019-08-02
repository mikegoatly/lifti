namespace Lifti
{
    public class FullTextIndexOptions
    {
        public WordSplitOptions WordSplitOptions { get; set; } = new WordSplitOptions();
    }

    public class FullTextIndexOptions<TKey> : FullTextIndexOptions
    {
    }
}
