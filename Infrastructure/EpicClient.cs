using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using VictorNovember.Infrastructure.Models;
using VictorNovember.Interfaces;

namespace VictorNovember.Infrastructure;

public sealed class EpicClient : IEpicClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EpicClient> _logger;

    public EpicClient(HttpClient httpClient, ILogger<EpicClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EpicImage>> GetNaturalAsync(
        CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("natural", ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"EPIC request failed: {(int)response.StatusCode} {response.ReasonPhrase}");

        var images = await response.Content
            .ReadFromJsonAsync<List<EpicImage>>(cancellationToken: ct);

        return images ?? throw new InvalidOperationException("EPIC returned null.");
    }
}
