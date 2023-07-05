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
            public TestObject(int id, string details, IDictionary<string, string> data)
            {
                this.Id = id;
                this.Details = details;
                this.Data = data;
            }

            public int Id { get; set; }
            public string Details { get; }
            public IDictionary<string, string> Data { get; set; }
        }

        public override async Task RunAsync()
        {
            Console.WriteLine("Creating an index that has dynamically registered fields for an object.");
            Console.WriteLine("Only one field, Details, is statically registered when the index is created.");

            var objects = new Dictionary<int, TestObject>
            {
                {
                    1,
                    new TestObject(
                        1,
                        "Some details",
                        new Dictionary<string, string> { { "Name", "Joe Bloggs" }, { "Profile", "Just placeholder text here" } })
                },
                {
                    2,
                    new TestObject(
                        2,
                        "Chillin with orange juice",
                        new Dictionary<string, string> { { "Name", "Just Bob" }, { "FavouriteExercise", "Jumping jacks" } })
                }
            };

            var index = new FullTextIndexBuilder<int>()
                .WithObjectTokenization<TestObject>(o => o
                    .WithKey(c => c.Id)
                    .WithField("Details", x => x.Details)
                    .WithDynamicFields("Data", c => c.Data)
                )
                .Build();

            await index.AddRangeAsync(objects.Values);

            var results = RunSearchAsync(
                index,
                "ju*",
                i => objects[i],
                @"Words beginning with 'ju' are contained across 4 fields, 3 of which will have been dynamically registered");

            Console.WriteLine("Fields known to the index:");
            foreach (var field in index.FieldLookup.AllFieldNames)
            {
                Console.WriteLine($"{field} - Field kind:{index.FieldLookup.GetFieldInfo(field).FieldKind}");
            }

            WaitForEnterToReturnToMenu();
        }
    }
}
