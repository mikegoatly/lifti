using Lifti;
using Lifti.Tokenization;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole
{
    public class FirstThreeLettersStemmer : IStemmer
    {
        public bool RequiresCaseInsensitivity => false;

        public bool RequiresAccentInsensitivity => false;

        public void Stem(StringBuilder builder)
        {
            if (builder.Length > 3)
            {
                builder.Length = 3;
            }
        }
    }

    public class CustomStemmerSample : SampleBase
    {
        public override async Task RunAsync()
        {
            var index = new FullTextIndexBuilder<int>()
                .WithDefaultTokenization(o => o.WithStemming(new FirstThreeLettersStemmer()))
                .Build();

            await index.AddAsync(1, "Some words");
            await index.AddAsync(2, "Wordy text");

            var results = index.Search("word");

            RunSearch(
                index,
                "word",
                "Searching for 'word' will get stemmed to just the first three characters, so will match both items");

            WaitForEnterToReturnToMenu();
        }
    }
}
