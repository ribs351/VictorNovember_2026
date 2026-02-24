using VictorNovember.Services.BraveSearch.Models;

public interface IBraveSearchClient
{
    Task<SearchResult> SearchAsync(string query, CancellationToken ct);
}