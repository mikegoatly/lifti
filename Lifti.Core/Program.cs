using System;

namespace Lifti
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var input = "This is a test This is a test This is a test  This is a test some other words that might a b c d e make a collision but are unlikely";

            var index = new FullTextIndex<string>();

            index.Index("1", input);

            Console.WriteLine(index.Root.ToString());

            index.Index("2", input);

            Console.WriteLine(index.Root.ToString());

            foreach (var result in index.Search("test"))
            {
                Console.WriteLine(result);
            }
        }
    }
}
