using Lifti;
using Lifti.Serialization.Binary;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TestConsole
{
    public class IndexSerializationWithCustomKeySerializer
    {
        public static async Task RunAsync()
        {
            // Create a full text index with a custom key type
            var index = new FullTextIndexBuilder<CompositeKey>().Build();

            // Index some sample data
            await index.AddAsync(new CompositeKey(1, 9), "This is some text associated with A: fizz");
            await index.AddAsync(new CompositeKey(2, 9), "Some buzz text for B");
            await index.AddAsync(new CompositeKey(3, 11), "Text associated with C is both fizz and buzz");

            // This would error with: No standard key serializer exists for type CompositeKey - 
            //    please provide a custom implementation of IKeySerializer<> when serializing/deserializing.
            // var serializer = new BinarySerializer<int>();

            var serializer = new BinarySerializer<CompositeKey>(new CompositeKeySerializer());
            using var stream = new MemoryStream();

            // Serialize the index
            await serializer.SerializeAsync(index, stream, disposeStream: false);

            // Deserialize the index into a new instance
            stream.Position = 0;
            var newIndex = new FullTextIndexBuilder<CompositeKey>().Build();
            await serializer.DeserializeAsync(newIndex, stream, disposeStream: false);

            // Prove that the new index has the same contents and the keys have round-tripped
            // Emits: only (3, 11) contains Fizz & Buzz
            var match = newIndex.Search("fizz & buzz").Single();
            Console.WriteLine($"Only ({match.Key.UserId}, {match.Key.CompanyId}) contains Fizz & Buzz");
        }
    }
}
