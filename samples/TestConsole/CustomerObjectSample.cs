using Lifti;
using Lifti.Tokenization.TextExtraction;
using System;
using System.Threading.Tasks;

namespace TestConsole
{
    public class CustomerObjectSample : SampleBase
    {
        public class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string ProfileHtml { get; set; }
        }

        public override async Task RunAsync()
        {
            Console.WriteLine("Creating an index for a Customer object, with two fields, Name and Profile");

            var index = new FullTextIndexBuilder<int>()
                .WithObjectTokenization<Customer>(o => o
                    .WithKey(c => c.Id)
                    .WithField("Name", c => c.Name)
                    .WithField("Profile", c => c.ProfileHtml, textExtractor: new XmlTextExtractor())
                )
                .Build();

            await index.AddAsync(new Customer { Id = 1, Name = "Joe Bloggs", ProfileHtml = "<a>Something else something</a>" });
            await index.AddAsync(new Customer { Id = 2, Name = "Joe Something", ProfileHtml = "<a>Something else</a>" });

            var results = RunSearch(
                index, 
                "something", 
                @"Searching for 'Something' will result in ID 2 being ordered before ID 1.  
'Something' appears twice in each document overall, however document 2 has fewer words, therefore the matches are more statistically significant");

            Console.WriteLine("But if you only consider the 'Profile' field, then 'Something' only appears once in document 2, therefore document 1 will come first.");
            Console.WriteLine("Re-ordering search results by only the Profile field (overall scores are not affected)");
            results = results.OrderByField("Profile");
            PrintSearchResults(results);

            WaitForEnterToReturnToMenu();
        }
    }
}
