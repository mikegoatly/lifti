using System;
using System.Linq;

namespace Lifti
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var input = "This is a test This is a test This is a test  This is a test some other words that might a b c d e make a collision but are unlikely";
            var words = new BasicSplitter().Process(input);

            foreach (var word in words)
            {
                Console.WriteLine($"{new string(word.Word.ToArray())}: {string.Join(' ', word.Locations.Select(l => l.ToString()))}");
            }
        }
    }
}
