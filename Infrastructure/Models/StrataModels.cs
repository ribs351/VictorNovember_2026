using System.Text.Json.Serialization;

namespace VictorNovember.Infrastructure.Models;

public sealed class StrataChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<StrataChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;

    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxTokens { get; set; }
}

public sealed class StrataChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty; // "system" | "user" | "assistant"

    [JsonPropertyName("content")]
    public object? Content { get; set; }

    public static StrataChatMessage Text(string role, string text) =>
        new() { Role = role, Content = text };

    public static StrataChatMessage TextWithImage(string role, string text, string imageDataUrl) =>
        new()
        {
            Role = role,
            Content = new List<StrataContentPart>
            {
                StrataContentPart.FromText(text),
                StrataContentPart.FromImageUrl(imageDataUrl)
            }
        };
}

public sealed class StrataContentPart
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text"; // "text" | "image_url"

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonPropertyName("image_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StrataImageUrl? ImageUrl { get; set; }

    public static StrataContentPart FromText(string text) =>
        new() { Type = "text", Text = text };

    public static StrataContentPart FromImageUrl(string url) =>
        new() { Type = "image_url", ImageUrl = new StrataImageUrl { Url = url } };
}

public sealed class StrataImageUrl
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public sealed class StrataChatResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("choices")]
    public List<StrataChoice>? Choices { get; set; }

    [JsonPropertyName("usage")]
    public StrataUsage? Usage { get; set; }
}

public sealed class StrataChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public StrataResponseMessage? Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public sealed class StrataResponseMessage
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

public sealed class StrataUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}