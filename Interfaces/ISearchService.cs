using VictorNovember.Services.BraveSearch.Models;

namespace VictorNovember.Interfaces;

public interface ISearchService
{
    Task<SearchResult> SearchAsync(string query, CancellationToken ct = default);
}
