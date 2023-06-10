using Lifti;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestConsole
{
    public class DynamicFieldsSample : SampleBase
    {
        public class TestObject
        {
            public TestObject(int id, IDictionary<string, string> data)
            {
                this.Id = id;
                this.Data = data;
            }

            public int Id { get; set; }
            public IDictionary<string, string> Data { get; set; }
        }

        public override async Task RunAsync()
        {
            Console.WriteLine("Creating an index for a Customer object, with two fields, Name and Profile");

            var objects = new Dictionary<int, TestObject>
            {
                { 1, new TestObject(1, new Dictionary<string, string> { { "Name", "Joe Bloggs" }, { "Profile", "Just placeholder text here" } }) },
                { 2, new TestObject(2, new Dictionary<string, string> { { "Name", "Just Bob" }, { "FavouriteExercise", "Jumping jacks" } }) }
            };

            var index = new FullTextIndexBuilder<int>()
                .WithObjectTokenization<TestObject>(o => o
                    .WithKey(c => c.Id)
                    .WithDynamicFields(c => c.Data)
                )
                .Build();

            await index.AddRangeAsync(objects.Values);

            var results = RunSearchAsync(
                index,
                "ju*",
                i => objects[i],
                @"Words beginning with 'ju' are contained in both documents across 3 unique fields");

            Console.WriteLine("Dynamically registered fields:");
            foreach (var field in index.FieldLookup.AllFieldNames)
            {
                Console.WriteLine(field);
            }

            WaitForEnterToReturnToMenu();
        }
    }
}
