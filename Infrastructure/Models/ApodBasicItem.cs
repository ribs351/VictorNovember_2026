using System.Text.Json.Serialization;

namespace VictorNovember.Infrastructure.Models;

public sealed class ApodBasicItem
{
    [JsonPropertyName("date")] public string Date { get; set; } = "";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("media_type")] public string MediaType { get; set; } = "";
    [JsonPropertyName("explanation")] public string Explanation { get; set; } = "";
    [JsonPropertyName("copyright")] public string? Copyright { get; set; }
    [JsonPropertyName("url")] public string? PostUrl { get; set; }
    [JsonPropertyName("hdurl")] public string? HdUrl { get; set; }
}