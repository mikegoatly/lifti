using BlazorApp.Shared;
using Lifti;

namespace Blazor.Pages
{
    public partial class Index
    {
        //private readonly PageSummary[] defaultPages = new[]
        //{
        //    new PageSummary(0, "The_Three-Body_Problem_(novel)", ""),
        //    new PageSummary(0, "Aliens_(film)", ""),
        //    new PageSummary(0, "Porcupine_Tree", ""),
        //    new PageSummary(0, "Buffy_the_Vampire_Slayer", ""),
        //    new PageSummary(0, "Monstress_(comics)", ""),
        //    new PageSummary(0, "Donnie_Darko", ""),
        //    new PageSummary(0, "Zune", ""),
        //    new PageSummary(0, "The_Boys_(2019_TV_series)", ""),
        //    new PageSummary(0, "Game_of_Thrones", ""),
        //    new PageSummary(0, "Fantastic_Mr_Fox", "")
        //};
        private IList<SearchResult<string>>? results = null;
        private bool errored;
        private bool indexing;
        private string? selectedContent;
        private IReadOnlyList<TokenLocation>? wordLocations;
        private bool stemmingEnabled;
        private bool fuzzyMatchByDefault;

        private bool IndexRebuildRequired => this.fuzzyMatchByDefault != this.IndexService.FuzzyMatchByDefault || this.stemmingEnabled != this.IndexService.StemmingEnabled;

        private string? IndexText { get; set; }

        private string? SyntaxError { get; set; }

        private string? Message { get; set; }

        private string? SearchText { get; set; }

        public bool StemmingEnabled
        {
            get => this.stemmingEnabled;

            set
            {
                this.stemmingEnabled = value;
                this.StateHasChanged();
            }
        }

        public bool FuzzyMatchByDefault
        {
            get => this.fuzzyMatchByDefault;

            set
            {
                this.fuzzyMatchByDefault = value;
                this.StateHasChanged();
            }
        }

        protected override async Task OnInitializedAsync()
        {
            this.StemmingEnabled = this.IndexService.StemmingEnabled;
            this.FuzzyMatchByDefault = this.IndexService.FuzzyMatchByDefault;

            await this.RebuildIndex();
        }

        private async Task RebuildIndex()
        {
            this.Message = "Rebuilding index...";
            this.IndexService.FuzzyMatchByDefault = this.FuzzyMatchByDefault;
            this.IndexService.StemmingEnabled = this.StemmingEnabled;
            this.StateHasChanged();

            // Give the UI chance to update
            await Task.Delay(10);

            await this.IndexService.BuildIndexAsync();
            this.Message = "Ready";
            this.StateHasChanged();
        }

        private void ShowItem(SearchResult<string> searchResult)
        {
            var builder = new System.Text.StringBuilder(this.IndexService.GetSourceText(searchResult.Key));
            var locations = searchResult.FieldMatches.SelectMany(m => m.Locations).ToList();
            foreach (var location in locations.OrderByDescending(l => l.Start))
            {
                builder.Insert(location.Start + location.Length, "</span>");
                builder.Insert(location.Start, "<span class='bg-warning'>");
            }

            this.wordLocations = locations;
            this.selectedContent = builder.ToString();
        }

        private async Task IndexRandomPagesAsync()
        {
            this.errored = false;
            this.Message = "Getting 10 random Wikipedia pages...";
            try
            {
                var randomList = await this.WikipediaPageProvider.GetRandomPagesAsync();
                await this.FetchPagesAsync(randomList);
            }
            catch (Exception ex)
            {
                this.Message = ex.Message;
                this.errored = true;
            }
        }

        private async Task FetchPagesAsync(IEnumerable<PageSummary> pages)
        {
            this.indexing = true;
            this.errored = false;
            try
            {
                var i = 1;
                foreach (var result in pages)
                {
                    var counter = $"[{i++}/10]";
                    this.Message = $"{counter} Fetching page " + ((result.Title?.Length ?? 0) > 0 ? result.Title : result.Slug);
                    StateHasChanged();
                    var pageContent = await this.WikipediaPageProvider.GetPageContentAsync(result);
                    this.Message = $"{counter} Indexing...";

                    StateHasChanged();

                    // Give the UI chance to update
                    await Task.Delay(10);

                    await this.IndexService.AddAsync(pageContent);
                }

                this.Message = "Ready";
            }
            catch (Exception ex)
            {
                this.Message = ex.Message;
                this.errored = true;
            }
            finally
            {
                this.indexing = false;
            }
        }

        private void ShowIndex()
        {
            this.IndexText = this.IndexService.GetIndexTextualRepresentation();
        }

        private void Clear()
        {
            this.IndexText = null;
            this.results = null;
        }

        private void Search()
        {
            try
            {
                if (this.SearchText == null)
                {
                    return;
                }

                this.IndexText = null;
                this.SyntaxError = null;
                this.results = this.IndexService.Search(this.SearchText);
            }
            catch (Exception ex)
            {
                this.SyntaxError = ex.Message;
            }
        }

        private void ShowAll()
        {
            this.IndexText = null;
            this.results = this.IndexService.GetIndexedKeys().Select(k => new SearchResult<string>(k, Array.Empty<FieldSearchResult>())).ToList();
        }
    }
}