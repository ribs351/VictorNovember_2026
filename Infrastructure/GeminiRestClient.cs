using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VictorNovember.Infrastructure;

public sealed class GenerateContentRequest
{
    [JsonPropertyName("contents")]
    public List<Content> Contents { get; set; } = new();

    [JsonPropertyName("generationConfig")]
    public GenerationConfig? GenerationConfig { get; set; }
}

public sealed class Content
{
    [JsonPropertyName("role")]
    public string? Role { get; set; } // "user" | "model"

    [JsonPropertyName("parts")]
    public List<Part> Parts { get; set; } = new();
}

public sealed class Part
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("inlineData")]
    public InlineData? InlineData { get; set; }
    [JsonPropertyName("thought")]
    public bool? Thought { get; set; }
}

public sealed class InlineData
{
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = "image/png";

    [JsonPropertyName("data")]
    public string Data { get; set; } = ""; // base64
}

public sealed class ThinkingConfig
{
    [JsonPropertyName("thinkingLevel")]
    public string? ThinkingLevel { get; set; } // e.g. "low", "high", "minimal"
}

public sealed class GenerationConfig
{
    [JsonPropertyName("thinkingConfig")]
    public ThinkingConfig? ThinkingConfig { get; set; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("maxOutputTokens")]
    public int? MaxOutputTokens { get; set; }

    [JsonPropertyName("responseMimeType")]
    public string? ResponseMimeType { get; set; } // e.g. "application/json"
}

public sealed class GenerateContentResponse
{
    [JsonPropertyName("candidates")]
    public List<Candidate>? Candidates { get; set; }

    // Automatically filters out parts where Thought is true and returning only the final answer!
    public string? Text() =>
        Candidates?.FirstOrDefault()?.Content?.Parts?
            .Where(p => p.Thought != true && p.Text is not null)
            .FirstOrDefault()?.Text;
}

public sealed class UsageMetadata
{
    [JsonPropertyName("thoughtsTokenCount")]
    public int ThoughtsTokenCount { get; set; }
}

public sealed class Candidate
{
    [JsonPropertyName("content")]
    public Content? Content { get; set; }

    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }
}

public sealed class GeminiOverloadedException : Exception
{
    public int StatusCode { get; }
    public GeminiOverloadedException(int statusCode, string message) : base(message)
        => StatusCode = statusCode;
}

public sealed class GeminiApiException : Exception
{
    public int StatusCode { get; }
    public GeminiApiException(int statusCode, string message) : base(message)
        => StatusCode = statusCode;
}

/// <summary>
/// Bare-metal REST client for the Gemini API
/// </summary>
public sealed class GeminiRestClient
{
    private const string ModelsPath = "v1beta/models";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly string _apiKey;

    public GeminiRestClient(HttpClient httpClient, string apiKey)
    {
        _http = httpClient;
        _apiKey = apiKey;
    }

    public async Task<GenerateContentResponse> GenerateContentAsync(
        string model,
        string prompt,
        GenerationConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        var request = new GenerateContentRequest
        {
            Contents = new()
            {
                new Content
                {
                    Role = "user",
                    Parts = new() { new Part { Text = prompt } }
                }
            },
            GenerationConfig = config
        };

        return await SendAsync(model, request, cancellationToken);
    }

    public async Task<GenerateContentResponse> GenerateContentAsync(
        string model,
        string prompt,
        string imageUrl,
        string mimeType,
        GenerationConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        // Download and inline the image as base64
        var imageBytes = await _http.GetByteArrayAsync(imageUrl, cancellationToken);
        var base64 = Convert.ToBase64String(imageBytes);

        var request = new GenerateContentRequest
        {
            Contents = new()
            {
                new Content
                {
                    Role = "user",
                    Parts = new()
                    {
                        new Part { Text = prompt },
                        new Part { InlineData = new InlineData { MimeType = mimeType, Data = base64 } }
                    }
                }
            },
            GenerationConfig = config
        };

        return await SendAsync(model, request, cancellationToken);
    }

    private async Task<GenerateContentResponse> SendAsync(
        string model,
        GenerateContentRequest request,
        CancellationToken cancellationToken)
    {
        var url = $"{ModelsPath}/{model}:generateContent";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("x-goog-api-key", _apiKey);

        var json = JsonSerializer.Serialize(request, JsonOptions);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            if (statusCode == 503 || statusCode == 429 || statusCode == 500)
                throw new GeminiOverloadedException(statusCode, body);

            throw new GeminiApiException(statusCode, body);
        }

        return JsonSerializer.Deserialize<GenerateContentResponse>(body, JsonOptions)
            ?? throw new GeminiApiException(0, "Empty or unparseable response body.");
    }
}
