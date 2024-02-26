using BlazorApp.Shared;
using Lifti;
using Lifti.Tokenization.TextExtraction;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BlazorApp.Services
{
    public class WikipediaIndexService
    {
        private static readonly Regex styleRegexReplacer = new Regex(@"<style[^>]*>[\s\S]*?</style>", RegexOptions.Compiled);
        private readonly Dictionary<string, WikipediaPage> loadedPages;

        public WikipediaIndexService()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Blazor.Services.InitialPages.dat");
            if (stream == null)
            {
                throw new Exception("Unable to load initial pages from embedded resource");
            }

            using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
            var deserializedPages = JsonSerializer.Deserialize<Dictionary<string, WikipediaPage>>(gzipStream);
            if (deserializedPages == null)
            {
                throw new Exception("Unable to deserialize initial pages");
            }

            this.loadedPages = deserializedPages;
        }

        public bool StemmingEnabled { get; set; }
        public bool FuzzyMatchByDefault { get; set; } = true;

        private FullTextIndex<string>? index;

        private FullTextIndex<string> Index => this.index ?? throw new Exception("Index hasn't been built!");

        public async Task BuildIndexAsync()
        {
            this.index = new FullTextIndexBuilder<string>()
                .WithDefaultTokenization(o => (this.StemmingEnabled ? o.WithStemming() : o).IgnoreCharacters('\''))
                .WithObjectTokenization<WikipediaPage>(
                    page => page.WithKey(i => i.Title)
                        .WithField(
                            name: "Content",
                            fieldTextReader: i => i.Text.Content,
                            tokenizationOptions: o => (this.StemmingEnabled ? o.WithStemming() : o).IgnoreCharacters('\''),
                            textExtractor: new XmlTextExtractor()))
                .WithQueryParser(o => this.FuzzyMatchByDefault ? o.AssumeFuzzySearchTerms() : o)
                .Build();

            if (this.loadedPages.Count > 0)
            {
                this.index.BeginBatchChange();

                foreach (var page in this.loadedPages.Values)
                {
                    await this.index.AddAsync(page);
                }

                await this.index.CommitBatchChangeAsync();
            }
        }

        public int IndexCount => this.index?.Count ?? 0;

        public async Task AddAsync(WikipediaPage page)
        {
            page.Text.Content = styleRegexReplacer.Replace(page.Text.Content, string.Empty);

            var index = this.Index;

            this.loadedPages[page.Title] = page;
            await index.AddAsync(page);
        }

        public string GetSourceText(string key)
        {
            return this.loadedPages[key].Text.Content;
        }

        public ISearchResults<string> Search(string query)
        {
            return this.Index.Search(query, QueryExecutionOptions.IncludeExecutionPlan);
        }

        public IEnumerable<string> GetIndexedKeys()
        {
            return this.Index.Metadata.GetIndexedDocuments().Select(i => i.Key);
        }

        public string GetIndexTextualRepresentation()
        {
            // return this.Index.ToString();

            return JsonSerializer.Serialize(this.loadedPages, new JsonSerializerOptions() { WriteIndented = true });
        }

        internal IndexNode? GetIndexRoot()
        {
            return this.index?.Root;
        }
    }
}
