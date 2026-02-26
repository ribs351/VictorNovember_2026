using VictorNovember.Services.BraveSearch.Models;

namespace VictorNovember.Interfaces;

public interface ISearchService
{
    Task<SearchResult> SearchWebAsync(string query, CancellationToken ct = default);
}
