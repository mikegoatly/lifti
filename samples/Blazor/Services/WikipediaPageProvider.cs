using BlazorApp.Shared;
using System.Net.Http.Json;

namespace BlazorApp.Services
{
    public class WikipediaPageProvider
    {
        private readonly HttpClient httpClient;

        public WikipediaPageProvider(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<IReadOnlyList<PageSummary>> GetRandomPagesAsync(int count)
        {
            var randomList = await this.httpClient.GetFromJsonAsync<RandomResult>($"https://en.wikipedia.org/w/api.php?action=query&list=random&rnlimit={count}&rnnamespace=0&format=json&&origin=*");

            if (randomList == null)
            {
                throw new Exception("No results deserialized!");
            }

            return randomList.Query.Random;
        }

        public async Task<WikipediaPage> GetPageContentAsync(PageSummary pageSummary)
        {
            var filter = pageSummary.Slug != null ? "page=" + pageSummary.Slug : "pageid=" + pageSummary.Id;
            var results = await this.httpClient.GetFromJsonAsync<WikipediaResult>($"https://en.wikipedia.org/w/api.php?action=parse&section=0&prop=text&{filter}&format=json&&origin=*");

            if (results == null)
            {
                throw new Exception("No page deserialized!");
            }

            return results.Page;
        }
    }
}
