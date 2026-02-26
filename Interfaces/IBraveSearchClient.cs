using VictorNovember.Services.BraveSearch.Models;

public interface IBraveSearchClient
{
    Task<SearchResult> SearchWebAsync(string query, CancellationToken ct = default);
}