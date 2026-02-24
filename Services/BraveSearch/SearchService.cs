using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using VictorNovember.Interfaces;
using VictorNovember.Services.BraveSearch.Models;

namespace VictorNovember.Services.BraveSearch;

public sealed class SearchService : ISearchService
{
    private readonly IBraveSearchClient _client;
    private readonly IMemoryCache _cache;
    private readonly ISearchUsageTracker _usage;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        IBraveSearchClient client,
        IMemoryCache cache,
        ISearchUsageTracker usage,
        ILogger<SearchService> logger)
    {
        _client = client;
        _cache = cache;
        _usage = usage;
        _logger = logger;
    }

    public async Task<SearchResult> SearchAsync(string query, CancellationToken ct)
    {
        // TODO: request coalescing (in-flight deduplication)
        var cacheKey = $"search:{query.ToLowerInvariant()}";

        if (_cache.TryGetValue(cacheKey, out SearchResult cached))
            return cached;

        if (!await _usage.TryAndIncrementAsync(ct))
            throw new InvalidOperationException("Monthly search quota reached.");

        var result = await _client.SearchAsync(query, ct);

        _cache.Set(cacheKey, result, TimeSpan.FromHours(12));

        return result;
    }
}
