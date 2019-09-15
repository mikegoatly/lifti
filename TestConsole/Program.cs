using Lifti;

namespace TestConsole
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var index = new FullTextIndex<string>(
                new FullTextIndexOptions<string>
                {
                    Advanced = { SupportIntraNodeTextAfterCharacterIndex = 4 }
                });
        }
    }
}
