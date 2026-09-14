using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using VictorNovember.Infrastructure.Models;
using VictorNovember.Interfaces;
using static VictorNovember.Enums.LLMServiceEnums;

namespace VictorNovember.Services;

public sealed class OllamaLLMService : ILlmService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<OllamaLLMService> _logger;
    private readonly IPromptProviderService _promptProviderService;
    private readonly string _model;
    private readonly bool _supportsVision;

    public OllamaLLMService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<OllamaLLMService> logger,
        IPromptProviderService promptProviderService)
    {
        _http = httpClient;
        _logger = logger;
        _promptProviderService = promptProviderService;

        _model = config["Ollama:Model"] ?? "qwen3.5:2b";

        _supportsVision = config.GetValue<bool>("Ollama:SupportsVision");
    }

    public async Task<string> GenerateTextAsync(string query, PromptMode promptMode, CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt(promptMode);

        var request = new OllamaChatRequest
        {
            Model = _model,
            Stream = false,
            Messages = new List<OllamaChatMessage>
            {
                new() { Role = "system", Content = systemPrompt },
                new() { Role = "user", Content = query }
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
                "Model {Model} is not configured for vision (Ollama:SupportsVision=false). Falling back to text-only commentary using the caption alone.",
                _model);

            var textOnlyRequest = new OllamaChatRequest
            {
                Model = _model,
                Stream = false,
                Messages = new List<OllamaChatMessage>
                {
                    new() { Role = "system", Content = systemPrompt },
                    new() { Role = "user", Content = userPrompt }
                }
            };

            return await SendAsync(textOnlyRequest, "Vision (fallback: text-only)", cancellationToken);
        }

        var imageBytes = await _http.GetByteArrayAsync(imageUrl, cancellationToken);
        var base64 = Convert.ToBase64String(imageBytes);

        var request = new OllamaChatRequest
        {
            Model = _model,
            Stream = false,
            Messages = new List<OllamaChatMessage>
            {
                new() { Role = "system", Content = systemPrompt },
                new() { Role = "user", Content = userPrompt, Images = new List<string> { base64 } }
            }
        };

        return await SendAsync(request, "Vision", cancellationToken);
    }

    private async Task<string> SendAsync(OllamaChatRequest request, string logPrefix, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync("api/chat", request, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"Ollama request failed: {(int)response.StatusCode} {response.ReasonPhrase} - {body}");
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(JsonOptions, cancellationToken);

            return result?.Message?.Content ?? string.Empty;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "{Prefix} Ollama generation failed for model {Model}.", logPrefix, _model);
            throw;
        }
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