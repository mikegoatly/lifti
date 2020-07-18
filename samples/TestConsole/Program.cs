using System.Threading.Tasks;

namespace TestConsole
{
    internal class Program
    {
        public static async Task Main()
        {
            await IndexSerializationWithCustomKeySerializer.RunAsync();
        }
    }
}
