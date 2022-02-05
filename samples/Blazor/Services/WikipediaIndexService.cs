using BlazorApp.Shared;
using Lifti;
using Lifti.Tokenization.TextExtraction;
using System.Reflection;
using System.Text.Json;

namespace BlazorApp.Services
{

    public class WikipediaIndexService
    {
        private readonly Dictionary<string, WikipediaPage> loadedPages;

        public WikipediaIndexService()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Blazor.Services.InitialPages.json");
            if (stream == null)
            {
                throw new Exception("Unable to load initial pages from embedded resource");
            }

            var deserializedPages = JsonSerializer.Deserialize<Dictionary<string, WikipediaPage>>(stream);
            if (deserializedPages == null)
            {
                throw new Exception("Unable to deserialize initial pages");
            }

            this.loadedPages = deserializedPages;
        }

        public bool StemmingEnabled { get; set; } = true;
        public bool FuzzyMatchByDefault { get; set; } = false;

        private FullTextIndex<string>? index;

        private FullTextIndex<string> Index => this.index ?? throw new Exception("Index hasn't been built!");

        public async Task BuildIndexAsync()
        {
            this.index = new FullTextIndexBuilder<string>()
                .WithDefaultTokenization(o => this.StemmingEnabled ? o.WithStemming() : o)
                .WithObjectTokenization<WikipediaPage>(
                    page => page.WithKey(i => i.Title)
                        .WithField(
                            name: "Content",
                            fieldTextReader: i => i.Text.Content,
                            tokenizationOptions: o => this.StemmingEnabled ? o.WithStemming() : o,
                            textExtractor: new XmlTextExtractor()))
                .WithQueryParser(o => this.FuzzyMatchByDefault ? o.AssumeFuzzySearchTerms() : o)
                .Build();

            if (this.loadedPages.Count > 0)
            {
                this.index.BeginBatchChange();

                foreach (var page in this.loadedPages.Values)
                {
                    await this.AddAsync(page);
                }

                await this.index.CommitBatchChangeAsync();
            }
        }

        public int IndexCount => this.index?.Count ?? 0;

        public async Task AddAsync(WikipediaPage page)
        {
            var index = this.Index;

            this.loadedPages[page.Title] = page;
            await index.AddAsync(page);
        }

        public string GetSourceText(string key)
        {
            return this.loadedPages[key].Text.Content;
        }

        public IList<SearchResult<string>> Search(string query)
        {
            return this.Index.Search(query).ToList();
        }

        public IEnumerable<string> GetIndexedKeys()
        {
            return this.Index.Items.GetIndexedItems().Select(i => i.Item);
        }

        public string GetIndexTextualRepresentation()
        {
            //return this.Index.ToString();

            return JsonSerializer.Serialize(this.loadedPages);
        }
    }
}
