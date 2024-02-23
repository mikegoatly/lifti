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
        private IList<SearchResult<string>>? results;
        private QueryExecutionPlan? executionPlan;
        private bool errored;
        private bool indexing;
        private SearchResult<string>? selectedSearchResult;
        private bool stemmingEnabled;
        private bool fuzzyMatchByDefault;

        private bool IndexRebuildRequired => this.fuzzyMatchByDefault != this.IndexService.FuzzyMatchByDefault || this.stemmingEnabled != this.IndexService.StemmingEnabled;

        private IndexNode? ShowingRootNode { get; set; }

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

        private async Task IndexRandomPagesAsync(int count = 10)
        {
            this.errored = false;
            this.Message = $"Getting {count} random Wikipedia pages...";
            try
            {
                var randomList = await this.WikipediaPageProvider.GetRandomPagesAsync(count);
                await this.FetchPagesAsync(randomList, count);
            }
            catch (Exception ex)
            {
                this.Message = ex.Message;
                this.errored = true;
            }
        }

        private async Task FetchPagesAsync(IEnumerable<PageSummary> pages, int count)
        {
            this.indexing = true;
            this.errored = false;
            try
            {
                var i = 1;
                foreach (var result in pages)
                {
                    var counter = $"[{i++}/{count}]";
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
            this.ShowingRootNode = this.IndexService.GetIndexRoot();
        }

        private void Clear()
        {
            this.ShowingRootNode = null;
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

                this.selectedSearchResult = null;
                this.ShowingRootNode = null;
                this.SyntaxError = null;
                var searchResults = this.IndexService.Search(this.SearchText);
                this.results = searchResults.ToList();
                this.executionPlan = searchResults.GetExecutionPlan();
            }
            catch (Exception ex)
            {
                this.SyntaxError = ex.Message;
            }
        }

        private void ShowAll()
        {
            this.ShowingRootNode = null;
            this.results = this.IndexService.GetIndexedKeys().Select(k => new SearchResult<string>(k, Array.Empty<FieldSearchResult>())).ToList();
        }
    }
}