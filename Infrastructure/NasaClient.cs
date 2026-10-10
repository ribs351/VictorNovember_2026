using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using VictorNovember.Infrastructure.Models;
using VictorNovember.Interfaces;

namespace VictorNovember.Infrastructure;

public class NasaClient : INasaClient
{
    private const string ApodBasicUrl = "https://science.nasa.gov/wp-json/wp/v2/apod-basic";

    private static readonly Regex TagPattern = new("<.*?>", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex TeaserPattern = new(@"\s*Tomorrow['’]s picture:.*$", RegexOptions.Singleline | RegexOptions.Compiled);

    private readonly HttpClient _httpClient;
    private readonly NasaOptions _options;

    public NasaClient(HttpClient httpClient, IOptions<NasaOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }
    public async Task<ApodResponse> GetApodAsync(DateTime? date = null, CancellationToken ct = default)
    {
        var code = (date ?? DateTime.UtcNow).ToString("yyMMdd");
        var url = date.HasValue
            ? $"{ApodBasicUrl}?per_page=1&date_from={code}&date_to={code}"
            : $"{ApodBasicUrl}?per_page=1";

        using var response = await _httpClient.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"NASA APOD request failed: {(int)response.StatusCode} {response.ReasonPhrase}");

        var items = await response.Content.ReadFromJsonAsync<List<ApodBasicItem>>(cancellationToken: ct);
        var item = items?.FirstOrDefault()
            ?? throw new InvalidOperationException("NASA APOD returned no entries.");

        return Map(item);
    }

    public async Task<NeoFeedResponse> GetNeoFeedAsync(DateTime? date = null, CancellationToken ct = default)
    {
        // start_date/end_date are the same day
        var targetDate = (date ?? DateTime.UtcNow).ToString("yyyy-MM-dd");
        var endpoint = $"neo/rest/v1/feed?start_date={targetDate}&end_date={targetDate}&api_key={_options.ApiKey}";

        var response = await _httpClient.GetAsync(endpoint, ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"NASA NeoWs request failed: {(int)response.StatusCode} {response.ReasonPhrase}");

        var feed = await response.Content.ReadFromJsonAsync<NeoFeedResponse>(cancellationToken: ct);

        return feed ?? throw new InvalidOperationException("NASA returned null.");
    }

    private static ApodResponse Map(ApodBasicItem item) => new()
    {
        Date = item.Date,
        Title = StripHtml(item.Title),
        Explanation = CleanExplanation(item.Explanation),
        PostUrl = item.PostUrl ?? "",
        HdUrl = string.IsNullOrWhiteSpace(item.HdUrl) ? null : item.HdUrl,
        MediaType = item.MediaType,
        Copyright = NullIfEmpty(StripHtml(item.Copyright)),
    };

    public static string StripHtml(string? html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        return WebUtility.HtmlDecode(TagPattern.Replace(html, "")).Trim();
    }

    public static string CleanExplanation(string? html) =>
        TeaserPattern.Replace(StripHtml(html), "").Trim();

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrEmpty(value) ? null : value;
}
