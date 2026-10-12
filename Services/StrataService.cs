using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using VictorNovember.Infrastructure.Models;
using VictorNovember.Interfaces;
using static VictorNovember.Enums.LLMServiceEnums;

namespace VictorNovember.Services;

public sealed class StrataLLMService : ILlmService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string ChatCompletionsPath = "v1/chat/completions";

    private readonly HttpClient _http;
    private readonly ILogger<StrataLLMService> _logger;
    private readonly IPromptProviderService _promptProviderService;
    private readonly string _model;
    private readonly string? _apiKey;
    private readonly bool _supportsVision;

    public StrataLLMService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<StrataLLMService> logger,
        IPromptProviderService promptProviderService)
    {
        _http = httpClient;
        _logger = logger;
        _promptProviderService = promptProviderService;

        // Strata serves a single model, but OpenAI-style APIs still expect a "model" field.
        _model = config["Strata:Model"] ?? "strata";
        _apiKey = config["Strata:ApiKey"];
        _supportsVision = config.GetValue<bool>("Strata:SupportsVision");
    }

    public async Task<string> GenerateTextAsync(string query, PromptMode promptMode, CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt(promptMode);

        var request = new StrataChatRequest
        {
            Model = _model,
            Stream = false,
            Messages = new List<StrataChatMessage>
            {
                StrataChatMessage.Text("system", systemPrompt),
                StrataChatMessage.Text("user", query)
            }
        };

        return await SendAsync(request, "Text-only", cancellationToken);
    }

    public async Task<string> GenerateVisionCommentaryAsync(string imageUrl, double? latitude, double? longitude, string caption, CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt(PromptMode.Technical);
        var userPrompt = BuildEPICUserPrompt(caption, latitude, longitude, imageAttached: _supportsVision);

        if (!_supportsVision)
        {
            _logger.LogWarning(
                "Strata is not configured for vision (Strata:SupportsVision=false). Falling back to text-only commentary using the caption alone.");

            var textOnlyRequest = new StrataChatRequest
            {
                Model = _model,
                Stream = false,
                Messages = new List<StrataChatMessage>
                {
                    StrataChatMessage.Text("system", systemPrompt),
                    StrataChatMessage.Text("user", userPrompt)
                }
            };

            return await SendAsync(textOnlyRequest, "Vision (fallback: text-only)", cancellationToken);
        }

        using var imageResponse = await _http.GetAsync(imageUrl, cancellationToken);
        imageResponse.EnsureSuccessStatusCode();

        var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        var mediaType = imageResponse.Content.Headers.ContentType?.MediaType;
        if (string.IsNullOrWhiteSpace(mediaType) || !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            mediaType = GuessImageMediaType(imageUrl);
        }

        var dataUrl = $"data:{mediaType};base64,{Convert.ToBase64String(imageBytes)}";

        var request = new StrataChatRequest
        {
            Model = _model,
            Stream = false,
            Messages = new List<StrataChatMessage>
            {
                StrataChatMessage.Text("system", systemPrompt),
                StrataChatMessage.TextWithImage("user", userPrompt, dataUrl)
            }
        };

        return await SendAsync(request, "Vision", cancellationToken);
    }

    private async Task<string> SendAsync(StrataChatRequest request, string logPrefix, CancellationToken cancellationToken)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ChatCompletionsPath)
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };

            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            }

            using var response = await _http.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"Strata request failed: {(int)response.StatusCode} {response.ReasonPhrase} - {body}");
            }

            var result = await response.Content.ReadFromJsonAsync<StrataChatResponse>(JsonOptions, cancellationToken);

            var choice = result?.Choices?.FirstOrDefault();

            if (choice?.FinishReason == "length")
            {
                _logger.LogWarning("{Prefix} Strata response was truncated (finish_reason=length).", logPrefix);
            }

            return choice?.Message?.Content ?? string.Empty;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "{Prefix} Strata generation failed for model {Model}.", logPrefix, _model);
            throw;
        }
    }

    private static string GuessImageMediaType(string imageUrl)
    {
        var path = Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ? uri.AbsolutePath : imageUrl;

        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };
    }

    private string BuildSystemPrompt(PromptMode mode)
    {
        var basePrompt = _promptProviderService.GetBasePrompt();
        var modeInstructions = _promptProviderService.GetModeInstructions(mode);

        return $"""
{basePrompt}
 
Additional instructions:
{modeInstructions}
""";
    }

    private static string BuildEPICUserPrompt(string caption, double? latitude, double? longitude, bool imageAttached)
    {
        var locationText = latitude.HasValue && longitude.HasValue
                            ? $"The image centroid is located at latitude {latitude:F4} and longitude {longitude:F4}."
                            : "No centroid coordinate data is available.";

        var framingLine = imageAttached
            ? "You are observing an official NASA EPIC Earth image."
            : "You are commenting on an official NASA EPIC Earth image based on its caption alone (no image data is available to you right now).";

        return $"""
{framingLine}
 
Caption: {caption}
{locationText}
 
Provide a concise, scientifically grounded commentary.
""";
    }
}