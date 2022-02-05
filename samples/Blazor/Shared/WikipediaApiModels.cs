using System.Text.Json.Serialization;

namespace BlazorApp.Shared
{
    public class WikipediaResult
    {
        public WikipediaResult(WikipediaPage page)
        {
            this.Page = page;
        }

        [JsonPropertyName("parse")]
        public WikipediaPage Page { get; }
    }

    public class WikipediaPage
    {
        public WikipediaPage(string title, int pageId, Text text)
        {
            this.Title = title;
            this.PageId = pageId;
            this.Text = text;
        }

        public string Title { get; }
        public int PageId { get; }
        public Text Text { get; }
    }

    public class Text
    {
        public Text(string content)
        {
            this.Content = content;
        }

        [JsonPropertyName("*")]
        public string Content { get; }
    }


    public class RandomResult
    {
        public RandomResult(Query query)
        {
            this.Query = query;
        }

        public Query Query { get; }
    }

    public class Query
    {
        public Query(IReadOnlyList<PageSummary> random)
        {
            this.Random = random;
        }

        [JsonPropertyName("random")]
        public IReadOnlyList<PageSummary> Random { get; }
    }

    public class PageSummary
    {
        public PageSummary(int? id, string slug, string title)
        {
            this.Id = id;
            this.Slug = slug;
            this.Title = title;
        }

        public int? Id { get; }
        public string Slug { get; }
        public string Title { get; }
    }

}
