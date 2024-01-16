using Lifti;
using Lifti.Serialization.Binary;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TestConsole
{
    public readonly record struct CompositeKey(int UserId, short CompanyId);

    public class CompositeKeySerializer : IKeySerializer<CompositeKey>
    {
        public void Write(BinaryWriter writer, CompositeKey key)
        {
            writer.Write(key.UserId); // Int32
            writer.Write(key.CompanyId); // Int16
        }

        public CompositeKey Read(BinaryReader reader)
        {
            // The serialization framework will make sure this method is only
            // ever called when a key is ready to be read.
            // Ensure the data is read is read out in exactly the same order and with the same 
            // data types it was written.
            var userId = reader.ReadInt32();
            var companyId = reader.ReadInt16();

            return new CompositeKey(userId, companyId);
        }
    }

    public class IndexSerializationWithCustomKeySerializer : SampleBase
    {
        public override async Task RunAsync()
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

            var match = index.Search("fizz & buzz").Single();
            Console.WriteLine($"Only ({match.Key.UserId}, {match.Key.CompanyId}) contains 'Fizz & Buzz' in the original index");

            var serializer = new BinarySerializer<CompositeKey>(new CompositeKeySerializer());
            using var stream = new MemoryStream();

            // Serialize the index
            Console.WriteLine("Serializing index using a custom key serializer");
            await serializer.SerializeAsync(index, stream, disposeStream: false);

            // Deserialize the index into a new instance
            Console.WriteLine("Deserializing to a new index");
            stream.Position = 0;
            var newIndex = new FullTextIndexBuilder<CompositeKey>().Build();
            await serializer.DeserializeAsync(newIndex, stream, disposeStream: false);

            // Prove that the new index has the same contents and the keys have round-tripped
            // Emits: only (3, 11) contains Fizz & Buzz
            match = newIndex.Search("fizz & buzz").Single();
            Console.WriteLine($"Only ({match.Key.UserId}, {match.Key.CompanyId}) contains 'Fizz & Buzz' in the deserialized index");

            WaitForEnterToReturnToMenu();
        }
    }
}
