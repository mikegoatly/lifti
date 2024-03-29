﻿using Lifti;
using Lifti.Serialization.Binary;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TestConsole
{
    public class IndexSerialization : SampleBase
    {
        public override async Task RunAsync()
        {
            // Create a full text index with default settings
            var index = new FullTextIndexBuilder<int>().Build();

            // Index some sample data
            await index.AddAsync(1, "This is some text associated with A: fizz");
            await index.AddAsync(2, "Some buzz text for B");
            await index.AddAsync(3, "Text associated with C is both fizz and buzz");

            var matches = index.Search("text");
            Console.WriteLine($"{matches.Count()} items contain 'text' in the original index");

            var serializer = new BinarySerializer<int>();
            using var stream = new MemoryStream();

            // Serialize the index
            Console.WriteLine("Serializing index");
            await serializer.SerializeAsync(index, stream, disposeStream: false);

            // Deserialize the index into a new instance
            Console.WriteLine("Deserializing to a new index");
            stream.Position = 0;
            var newIndex = new FullTextIndexBuilder<int>().Build();
            await serializer.DeserializeAsync(newIndex, stream, disposeStream: false);

            // Prove that the new index has the same contents
            // Emits: 3 items contain text in the new index
            matches = newIndex.Search("text");
            Console.WriteLine($"{matches.Count()} items contain 'text' in the new index");

            WaitForEnterToReturnToMenu();
        }
    }
}
