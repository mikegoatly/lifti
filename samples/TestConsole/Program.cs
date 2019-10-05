using Lifti;
using System;
using System.Linq;

namespace TestConsole
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Create a full text index with default settings
            var index = new FullTextIndex<string>();

            // Index
            index.Index("A", "This is some text associated with A: fizz");
            index.Index("B", "Some buzz text for B");
            index.Index("C", "Text associated with C is both fizz and buzz");

            // Search for text containing both Fizz *and* Buzz
            var results = index.Search("Fizz Buzz").ToList();

            // Output: Items with both Fizz and Buzz: 1
            Console.WriteLine($"Items with both Fizz and Buzz: {results.Count}");

            // Search for text containing both Fizz *or* Buzz
            results = index.Search("Fizz | Buzz").ToList();

            // Outputs: Items with Fizz or Buzz: 3
            Console.WriteLine($"Items with Fizz or Buzz: {results.Count}");
        }
    }
}
