using Lifti;
using System.Threading.Tasks;

namespace TestConsole
{
    public class SimpleTextSample : SampleBase
    {
        public override async Task RunAsync()
        {
            // Create a full text index with default settings
            var index = new FullTextIndexBuilder<string>().Build();

            // Index
            await index.AddAsync("A", "This is some text associated with A: fizz");
            await index.AddAsync("B", "Some buzz text for B");
            await index.AddAsync("C", "Text associated with C is both fizz and buzz");

            // Number of items with both Fizz and Buzz: 1
            RunSearch(index, "Fizz Buzz", "Items with both Fizz AND Buzz");

            // Number of items with Fizz or Buzz: 3
            RunSearch(index, "Fizz | Buzz", "Items with Fizz OR Buzz");

            WaitForEnterToReturnToMenu();
        }
    }
}
