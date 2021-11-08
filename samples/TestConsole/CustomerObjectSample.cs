using Lifti;
using Lifti.Tokenization.TextExtraction;
using System.Threading.Tasks;

namespace TestConsole
{
    public static class CustomerObjectSample
    {
        public class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string ProfileHtml { get; set; }
        }

        public static async Task RunAsync()
        {
            var index = new FullTextIndexBuilder<int>()
                .WithObjectTokenization<Customer>(o => o
                    .WithKey(c => c.Id)
                    .WithField("Name", c => c.Name)
                    .WithField("Profile", c => c.ProfileHtml, textExtractor: new XmlTextExtractor())
                )
                .Build();
        }
    }
}
