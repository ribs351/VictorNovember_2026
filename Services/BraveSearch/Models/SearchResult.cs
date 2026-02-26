namespace VictorNovember.Services.BraveSearch.Models;


public sealed class SearchResult
{
    public IReadOnlyList<SearchResultItem> Items { get; }

    public SearchResult(IReadOnlyList<SearchResultItem> items)
    {
        Items = items;
    }
}

public sealed class SearchResultItem
{
    public string Title { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Source { get; init; }
    public string? ThumbnailUrl { get; init; }
}
public sealed class BraveSearchResponse
{
    public BraveWebSection? Web { get; set; }
}

public sealed class BraveWebSection
{
    public List<BraveWebResult>? Results { get; set; }
}

public sealed class BraveWebResult
{
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? Description { get; set; }
    public BraveProfile? Profile { get; set; }
    public BraveThumbnail? Thumbnail { get; set; }
}

public sealed class BraveProfile
{
    public string? Name { get; set; }
}

public sealed class BraveThumbnail
{
    public string? Src { get; set; }
}