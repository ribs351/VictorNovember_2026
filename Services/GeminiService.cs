using GenerativeAI;
using GenerativeAI.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using VictorNovember.Interfaces;
using static VictorNovember.Enums.GeminiServiceEnums;

namespace VictorNovember.Services;

public sealed class GeminiService : IGeminiService
{
    private readonly GenerativeModel _primaryModel;
    private readonly GenerativeModel _fallbackModel;
    private readonly GenerativeModel _lastresortModel;
    private readonly ILogger<GeminiService> _logger;
    private readonly IPromptProviderService _promptProviderService;

    public GeminiService(IConfiguration config, ILogger<GeminiService> logger, IPromptProviderService promptProviderService)
    {
        _logger = logger;
        _promptProviderService = promptProviderService;
        var apiKey = config["GoogleAPIKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("GoogleAPIKey is missing.");

        var googleAI = new GoogleAi(apiKey);
        _primaryModel = googleAI.CreateGenerativeModel(GoogleAIModels.Gemmma3_27B);
        _fallbackModel = googleAI.CreateGenerativeModel(GoogleAIModels.Gemma3_12B);
        _lastresortModel = googleAI.CreateGenerativeModel(GoogleAIModels.Gemma3n_E4B);

        Configure(_primaryModel);
        Configure(_fallbackModel);
        Configure(_lastresortModel);
    }

    private static void Configure(GenerativeModel model)
    {
        model.UseGoogleSearch = false;
        model.UseGrounding = false;
        model.UseCodeExecutionTool = false;
    }

    public Task<string> GenerateTextAsync(string query, PromptMode promptMode, CancellationToken cancellationToken = default)
    {
        var prompt = BuildTextPrompt(query, promptMode);
        return ExecuteWithFallbackAsync(
            async (model, ct) => await TryGenerate(model, prompt, ct),
            "Text-only",
            cancellationToken);
    }

    public Task<string> GenerateVisionCommentaryAsync(string imageUrl, string caption, CancellationToken cancellationToken = default)
    {
        var prompt = BuildVisionPrompt(caption);
        return ExecuteWithFallbackAsync(
            async (model, ct) => await TryGenerate(model, prompt, imageUrl, ct),
            "Vision",
            cancellationToken);
    }

    private async Task<string> ExecuteWithFallbackAsync(
    Func<GenerativeModel, CancellationToken, Task<string?>> generator,
    string logPrefix,
    CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        int attempts = 0;
        string modelUsed = "primary";

        try
        {
            attempts++;
            var result = await generator(_primaryModel, cancellationToken);
            if (result is not null) return result;

            await Task.Delay(500, cancellationToken);
            attempts++;
            result = await generator(_primaryModel, cancellationToken);
            if (result is not null) return result;

            attempts++;
            modelUsed = "fallback";
            result = await generator(_fallbackModel, cancellationToken);
            if (result is not null) return result;

            attempts++;
            modelUsed = "lastResort";
            return await generator(_lastresortModel, cancellationToken)
                ?? string.Empty;
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

    private async Task<string?> TryGenerate(GenerativeModel model, string prompt, string path, CancellationToken ct)
    {
        try
        {
            return await GenerateWithModel(model, prompt, path, ct);
        }
        catch (Exception ex) when (IsOverloaded(ex))
        {
            return null;
        }
    }

    private async Task<string?> TryGenerate(GenerativeModel model, string prompt, CancellationToken ct)
    {
        try
        {
            return await GenerateWithModel(model, prompt, ct);
        }
        catch (Exception ex) when (IsOverloaded(ex))
        {
            return null;
        }
    }

    private static async Task<string> GenerateWithModel(
    GenerativeModel model,
    string prompt,
    string imageUrl,
    CancellationToken token)
    {
        var completion = await model.GenerateContentAsync(prompt, imageUrl, "image/png", cancellationToken: token)
            .ConfigureAwait(false);

        return completion.Text() ?? "";
    }

    private static async Task<string> GenerateWithModel(
    GenerativeModel model,
    string prompt,
    CancellationToken token)
    {
        var completion = await model.GenerateContentAsync(prompt, cancellationToken: token)
            .ConfigureAwait(false);

        return completion.Text() ?? "";
    }

    private string BuildVisionPrompt(string caption, PromptMode mode = PromptMode.InformativeReaction)
    {
        var basePrompt = _promptProviderService.GetBasePrompt();
        var modeInstructions = _promptProviderService.GetModeInstructions(mode);

        return $"""
{basePrompt}

Additional instructions:
{modeInstructions}

Describe the details of this uploaded image with the following caption: {caption}
""";
    }

    private string BuildTextPrompt(
    string query,
    PromptMode mode)
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

    private static bool IsOverloaded(Exception ex)
    {
        if (ex is ApiException apiEx)
            return apiEx.Message.Contains("(Code: 503)");

        if (ex.InnerException is ApiException inner)
            return inner.Message.Contains("(Code: 503)");

        return false;
    }
}
