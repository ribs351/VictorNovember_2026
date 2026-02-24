namespace VictorNovember.Services.BraveSearch.Models;

public sealed class SearchResult
{
    public string Query { get; init; } = string.Empty;
    public IReadOnlyList<SearchItem> Items { get; init; } = [];
}

public sealed class SearchItem
{
    public string Title { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}