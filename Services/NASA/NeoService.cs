using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using VictorNovember.Interfaces;
using VictorNovember.Services.NASA.Models;
using static VictorNovember.Enums.GeminiServiceEnums;

namespace VictorNovember.Services.NASA;

public sealed class NeoService : INeoService
{
    private readonly INasaClient _nasaClient;
    private readonly IGeminiService _geminiService;
    private readonly IMemoryCache _cache;

    public NeoService(INasaClient nasaClient, IGeminiService geminiService, IMemoryCache cache)
    {
        _nasaClient = nasaClient;
        _geminiService = geminiService;
        _cache = cache;
    }

    public async Task<NeoResult> GetClosestApproachTodayAsync(CancellationToken ct = default)
    {
        var feed = await _nasaClient.GetNeoFeedAsync(ct: ct);

        var asteroids = feed.AllAsteroids.ToList();

        if (asteroids.Count == 0)
            throw new InvalidOperationException("NASA returned no near-Earth objects for today.");

        // smallest miss distance among today's objects.
        var closest = asteroids
            .Where(a => a.PrimaryApproach is not null)
            .OrderBy(a => ParseDistanceKm(a.PrimaryApproach!.MissDistance.Kilometers))
            .First();

        var approach = closest.PrimaryApproach!;

        return new NeoResult(
            Name: closest.Name,
            NasaJplUrl: closest.NasaJplUrl,
            IsPotentiallyHazardous: closest.IsPotentiallyHazardous,
            DiameterMinMeters: closest.EstimatedDiameter.Meters.Min,
            DiameterMaxMeters: closest.EstimatedDiameter.Meters.Max,
            MissDistanceKm: ParseDistanceKm(approach.MissDistance.Kilometers),
            MissDistanceLunar: ParseDistanceKm(approach.MissDistance.Lunar),
            VelocityKmPerSecond: ParseDistanceKm(approach.RelativeVelocity.KilometersPerSecond),
            CloseApproachDate: approach.CloseApproachDate,
            OrbitingBody: approach.OrbitingBody,
            TotalObjectCountToday: asteroids.Count
        );
    }

    public async Task<string> GenerateCommentaryAsync(NeoResult neo)
    {
        var cacheKey = $"neo:commentary:{neo.CloseApproachDate}:{neo.Name}";

        try
        {
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

                var prompt = BuildPrompt(neo);

                var commentary = await _geminiService.GenerateTextAsync(
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

    private static double ParseDistanceKm(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

    private static string BuildPrompt(NeoResult neo)
    {
        var hazardNote = neo.IsPotentiallyHazardous
            ? "NASA classifies this object as potentially hazardous."
            : "NASA does not classify this object as hazardous.";

        return $"""
    React to today's closest near-Earth object flyby, as if it were a close call.

    Give a short in-character reaction to the following.

    Name: {neo.Name}
    Miss distance: {neo.MissDistanceKm:N0} km ({neo.MissDistanceLunar:N1} lunar distances)
    Estimated diameter: {neo.DiameterMinMeters:N0}-{neo.DiameterMaxMeters:N0} meters
    Velocity: {neo.VelocityKmPerSecond:N1} km/s
    {hazardNote}
    Total objects tracked today: {neo.TotalObjectCountToday}
    """;
    }
}