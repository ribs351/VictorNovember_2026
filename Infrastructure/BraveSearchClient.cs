using Microsoft.Extensions.Options;
using VictorNovember.Infrastructure;
using VictorNovember.Services.BraveSearch.Models;

public sealed class BraveSearchClient : IBraveSearchClient
{
    private readonly HttpClient _httpClient;
    private readonly BraveSearchOptions _options;

    public BraveSearchClient(
        HttpClient httpClient,
        IOptions<BraveSearchOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        _httpClient.DefaultRequestHeaders.Add("X-Subscription-Token", _options.ApiKey);
    }

    public async Task<SearchResult> SearchAsync(string query, CancellationToken ct)
    {
        var response = await _httpClient.GetAsync(
            $"web/search?q={Uri.EscapeDataString(query)}",
            ct);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);

        // JSON → SearchResult (refine parsing later)
        throw new NotImplementedException();
    }
}