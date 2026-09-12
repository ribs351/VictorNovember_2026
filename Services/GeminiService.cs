using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using VictorNovember.Infrastructure;
using VictorNovember.Interfaces;
using static VictorNovember.Enums.GeminiServiceEnums;

namespace VictorNovember.Services;

public sealed class GeminiService : IGeminiService
{
    private const string PrimaryModel = "gemma-4-26b-a4b-it";
    private const string FallbackModel = "gemma-4-31b-it";

    private readonly GeminiRestClient _client;
    private readonly ILogger<GeminiService> _logger;
    private readonly IPromptProviderService _promptProviderService;

    public GeminiService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<GeminiService> logger,
        IPromptProviderService promptProviderService)
    {
        _logger = logger;
        _promptProviderService = promptProviderService;

        var apiKey = config["GoogleAPIKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("GoogleAPIKey is missing.");

        _client = new GeminiRestClient(httpClient, apiKey);
    }

    public Task<string> GenerateTextAsync(string query, PromptMode promptMode, CancellationToken cancellationToken = default)
    {
        var prompt = BuildTextPrompt(query, promptMode);
        return ExecuteWithFallbackAsync(
            (model, ct) => TryGenerate(model, prompt, ct),
            "Text-only",
            cancellationToken);
    }

    public Task<string> GenerateVisionCommentaryAsync(string imageUrl, double? latitude, double? longitude, string caption, CancellationToken cancellationToken = default)
    {
        var prompt = BuildEPICPrompt(caption, latitude, longitude);
        return ExecuteWithFallbackAsync(
            (model, ct) => TryGenerate(model, prompt, imageUrl, ct),
            "Vision",
            cancellationToken);
    }

    private async Task<string> ExecuteWithFallbackAsync(
        Func<string, CancellationToken, Task<string?>> generator,
        string logPrefix,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        int attempts = 0;
        string modelUsed = "primary";

        try
        {
            attempts++;
            var result = await generator(PrimaryModel, cancellationToken);
            if (result is not null) return result;

            await Task.Delay(500, cancellationToken);
            attempts++;
            result = await generator(PrimaryModel, cancellationToken);
            if (result is not null) return result;

            attempts++;
            modelUsed = "fallback";
            return await generator(FallbackModel, cancellationToken) ?? string.Empty;
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation(
                "{Prefix} LLM: {Elapsed}ms | Model: {Model} | Attempts: {Attempts}",
                logPrefix,
                sw.ElapsedMilliseconds,
                modelUsed,
                attempts);
        }
    }

    private async Task<string?> TryGenerate(string model, string prompt, string imageUrl, CancellationToken ct)
    {
        try
        {
            var response = await _client.GenerateContentAsync(model, prompt, imageUrl, "image/png", cancellationToken: ct);
            return response.Text() ?? "";
        }
        catch (GeminiOverloadedException)
        {
            return null;
        }
    }

    private async Task<string?> TryGenerate(string model, string prompt, CancellationToken ct)
    {
        try
        {
            var response = await _client.GenerateContentAsync(model, prompt, cancellationToken: ct);
            return response.Text() ?? "";
        }
        catch (GeminiOverloadedException)
        {
            return null;
        }
    }

    private string BuildEPICPrompt(string caption, double? latitude, double? longitude, PromptMode mode = PromptMode.Technical)
    {
        var basePrompt = _promptProviderService.GetBasePrompt();
        var modeInstructions = _promptProviderService.GetModeInstructions(mode);
        var locationText = latitude.HasValue && longitude.HasValue
                            ? $"The image centroid is located at latitude {latitude:F4} and longitude {longitude:F4}."
                            : "No centroid coordinate data is available.";
        return $"""
{basePrompt}

Additional instructions:
{modeInstructions}

You are observing an official NASA EPIC Earth image.

Caption: {caption}
{locationText}

Provide a concise, scientifically grounded commentary.
""";
    }

    private string BuildTextPrompt(string query, PromptMode mode)
    {
        var basePrompt = _promptProviderService.GetBasePrompt();
        var modeInstructions = _promptProviderService.GetModeInstructions(mode);

        return $"""
{basePrompt}

Additional instructions:
{modeInstructions}

User message:
{query}

November:
""";
    }
}