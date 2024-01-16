using System.Net.Http.Json;

namespace UGIdotNET.SpikeTime.Dab.Client;

public class BooksApiClient(HttpClient http)
{
    public HttpClient Http { get; } = http;

    public async Task<BookListResult> GetBooksAsync(BookListQuery query)
    {
        var url = $"data-api/rest/Book?$orderby=year {query.Sort}";
        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            url = $"{url}&$filter=title eq '{query.Title}'";
        }

        var books = await Http.GetFromJsonAsync<BookListResult>(url);
        return books ?? new();
    }
}

public class BookListQuery
{
    public string Title { get; set; } = string.Empty;

    public string Sort { get; set; } = "desc";
}

public class BookListResult
{
    public IEnumerable<BookListItem> Value { get; set; } = [];

    public record BookListItem
    {
        public string Title { get; set; } = string.Empty;

        public int? Year { get; set; }

        public int? Pages { get; set; }
    }
}
