using Microsoft.Extensions.Caching.Memory;
using VictorNovember.Infrastructure.Models;
using VictorNovember.Interfaces;
using VictorNovember.Services.Fun.Models;
using static VictorNovember.Enums.GeminiServiceEnums;

namespace VictorNovember.Services.Fun;

public sealed class ApodService : IApodService
{
    private readonly INasaClient _nasaClient;
    private readonly IGeminiService _geminiService;
    private readonly IMemoryCache _cache;

    public ApodService(INasaClient nasaClient, IGeminiService geminiService, IMemoryCache cache)
    {
        _nasaClient = nasaClient;
        _geminiService = geminiService;
        _cache = cache;
    }

    public async Task<ApodResult> GetApodDataAsync(CancellationToken ct = default)
    {
        var apod = await _nasaClient.GetApodAsync(ct: ct);

        var imageUrl = ResolveImageUrl(apod);

        var trimmedExplanation = TrimExplanation(apod.Explanation);

        return new ApodResult(
            apod.Title,
            trimmedExplanation,
            imageUrl,
            apod.Date,
            apod.Copyright
        );
    }

    public async Task<string> GenerateCommentaryAsync(string title, string explanation, string apodDate)
    {
        var cacheKey = $"apod:commentary:{apodDate}";

        try
        {
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

                var prompt = BuildPrompt(title, explanation);

                var commentary = await _geminiService.GenerateAsync(
                    prompt,
                    PromptMode.InformativeReaction,
                    CancellationToken.None);

                return commentary;
            }) ?? string.Empty;
        }
        catch 
        {
            _cache.Remove(cacheKey);
            throw;
        }
    }

    private static string ResolveImageUrl(ApodResponse apod)
    {
        if (apod.MediaType == "image")
            return apod.HdUrl ?? apod.Url;

        return apod.ThumbnailUrl ?? apod.Url;
    }

    private static string TrimExplanation(string explanation)
    {
        const int maxLength = 1000;

        if (explanation.Length <= maxLength)
            return explanation;

        return explanation.Substring(0, maxLength) + "...";
    }

    public bool TryGetCachedCommentary(string date, out string commentary)
    {
        return _cache.TryGetValue($"apod:commentary:{date}", out commentary);
    }

    private static string BuildPrompt(string title, string explanation)
    {
        return $"""
    React to today's NASA Astronomy Picture of the Day.

    Give a short in-character reaction to the following.

    Title: {title}

    Description:
    {explanation}
    """;
    }
}
