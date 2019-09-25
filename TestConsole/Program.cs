using Lifti;

namespace TestConsole
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var index = new FullTextIndex<string>(
                new FullTextIndexConfiguration<string>
                {
                    Advanced = { SupportIntraNodeTextAfterCharacterIndex = 4 }
                });
        }
    }
}
