using System.Text.Json.Serialization;

namespace VictorNovember.Infrastructure.Models;

public sealed class ApodResponse
{
    public string Date { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Explanation { get; set; } = default!;

    public string Url { get; set; } = default!;

    [JsonPropertyName("hdurl")]
    public string? HdUrl { get; set; }

    [JsonPropertyName("media_type")]
    public string MediaType { get; set; } = default!;

    [JsonPropertyName("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }

    public string? Copyright { get; set; }

    [JsonPropertyName("service_version")]
    public string ServiceVersion { get; set; } = default!;
}