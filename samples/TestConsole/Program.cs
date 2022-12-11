using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace TestConsole
{
    internal static class Program
    {
        public static async Task Main()
        {
            do
            {
                var samples = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(t => !t.IsInterface && !t.IsAbstract && typeof(ISample).IsAssignableFrom(t))
                    .ToList();

                Console.Clear();
                Console.WriteLine("Select the sample to execute or Esc to exit:");
                Console.WriteLine();

                var firstLetter = 'a';
                var lastLetter = firstLetter;
                foreach (var sample in samples)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"{lastLetter}: ");
                    Console.ResetColor();
                    Console.WriteLine(sample.Name);
                    lastLetter++;
                }

                Console.WriteLine();

                char key;
                do
                {
                    var pressed = Console.ReadKey();
                    if (pressed.Key == ConsoleKey.Escape)
                    {
                        return;
                    }

                    key = char.ToLowerInvariant(pressed.KeyChar);

                    if (Console.CursorLeft > 0)
                    {
                        Console.CursorLeft -= 1;
                        Console.Write(' ');
                        Console.CursorLeft -= 1;
                    }
                } while (key < firstLetter|| key > lastLetter);

                var selectedSample = samples[key - 'a'];

                Console.Clear();
                Console.WriteLine($"Running {selectedSample.Name}");
                Console.WriteLine();

                await ((ISample)Activator.CreateInstance(selectedSample)).RunAsync();
            } while (true);
        }
    }
}
