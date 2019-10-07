using System.Text.Json.Serialization;

namespace BlazorApp.Shared
{
    public class WikipediaResult
    {
        [JsonPropertyName("parse")]
        public WikipediaPage Page { get; set; }
    }

    public class WikipediaPage
    {
        public string Title { get; set; }
        public int PageId { get; set; }
        public Text Text { get; set; }
    }

    public class Text
    {
        [JsonPropertyName("*")]
        public string Content { get; set; }
    }


    public class RandomResult
    {
        public Query Query { get; set; }
    }

    public class Query
    {
        public PageSummary[] random { get; set; }
    }

    public class PageSummary
    {
        public int? Id { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
    }

}
