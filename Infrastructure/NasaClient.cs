using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using VictorNovember.Infrastructure.Models;
using VictorNovember.Interfaces;

namespace VictorNovember.Infrastructure;

public class NasaClient : INasaClient
{
    private readonly HttpClient _httpClient;
    private readonly NasaOptions _options;

    public NasaClient(HttpClient httpClient, IOptions<NasaOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }
    public async Task<ApodResponse> GetApodAsync(DateTime? date = null, CancellationToken ct = default)
    {
        var endpoint = $"planetary/apod?api_key={_options.ApiKey}&thumbs=true";

        if (date.HasValue)
            endpoint += $"&date={date.Value:yyyy-MM-dd}";

        var response = await _httpClient.GetAsync(endpoint, ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"NASA APOD request failed: {(int)response.StatusCode} {response.ReasonPhrase}");

        var apod = await response.Content.ReadFromJsonAsync<ApodResponse>(cancellationToken: ct);

        return apod ?? throw new InvalidOperationException("NASA returned null.");
    }

    public Task<MarsRoverResponse> GetMarsPhotosAsync(CancellationToken ct = default)
        => throw new NotImplementedException();
}
