using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;
using VictorNovember.Infrastructure;
using VictorNovember.Services.BraveSearch.Models;

public sealed class BraveSearchClient : IBraveSearchClient
{
    private readonly HttpClient _httpClient;
    private readonly BraveSearchOptions _options;
    private readonly ILogger<BraveSearchClient> _logger;

    public BraveSearchClient(
        HttpClient httpClient,
        IOptions<BraveSearchOptions> options,
        ILogger<BraveSearchClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
            "X-Subscription-Token",
            _options.ApiKey);
    }

    public async Task<SearchResult> SearchWebAsync(string query, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"web/search?q={Uri.EscapeDataString(query)}&safesearch={_options.SafeSearch}",
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);

                _logger.LogWarning(
                    "Brave search failed. Status: {StatusCode}. Body: {Body}",
                    (int)response.StatusCode,
                    errorBody);

                response.EnsureSuccessStatusCode();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);

            var braveResponse = await JsonSerializer.DeserializeAsync<BraveSearchResponse>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                ct);

            if (braveResponse?.Web?.Results == null)
            {
                _logger.LogWarning(
                    "Brave returned null or empty results for query: {Query}",
                    query);

                return new SearchResult(Array.Empty<SearchResultItem>());
            }

            var results = braveResponse.Web.Results
                .Select(r => new SearchResultItem
                {
                    Title = r.Title ?? "No title",
                    Url = r.Url ?? string.Empty,
                    Description = StripHtml(r.Description ?? string.Empty),
                    Source = r.Profile?.Name,
                    ThumbnailUrl = r.Thumbnail?.Src
                })
                .ToList();

            _logger.LogInformation(
                "Brave search succeeded for query: {Query}. Results returned: {Count}",
                query,
                results.Count);

            return new SearchResult(results);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(
                ex,
                "Brave search timed out for query: {Query}",
                query);

            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Failed to deserialize Brave response for query: {Query}",
                query);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error during Brave search for query: {Query}",
                query);

            throw;
        }
    }

    private static string StripHtml(string input)
    {
        return Regex.Replace(input, "<.*?>", string.Empty);
    }
}