using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using VictorNovember.Interfaces;
using VictorNovember.Services.NASA;

namespace VictorNovember.ApplicationCommands;

public sealed class NASAModule : ApplicationCommandModule
{
    private readonly IApodService _apodService;
    private readonly IEpicService _epicService;
    private readonly ILogger<NASAModule> _logger;
    private readonly INeoService _neoService;
    public NASAModule(IApodService apodService, IEpicService epicService, ILogger<NASAModule> logger, INeoService neoService)
    {
        _apodService = apodService;
        _epicService = epicService;
        _logger = logger;
        _neoService = neoService;
    }

    [SlashCommand("apod", "Get NASA's Astronomy Picture of the Day")]
    [SlashCooldown(1, 10, SlashCooldownBucketType.Channel)]
    public async Task GetApodAsync(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        var apod = await _apodService.GetApodDataAsync();

        var embed = new DiscordEmbedBuilder()
            .WithTitle($"🌌 {apod.Title}")
            .WithImageUrl(apod.ImageUrl)
            .WithColor(DiscordColor.Azure)
            .WithFooter($"NASA APOD • {apod.Date}")
            .WithDescription("Generating commentary...");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));

        _ = Task.Run(async () =>
        {
            try
            {
                var commentary = await _apodService.GenerateCommentaryAsync(apod.Title, apod.TrimmedExplanation, apod.Date);

                embed.WithDescription($"November's commentary: {commentary}");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate commentary.");
            }
        });
    }

    [SlashCommand("earthimage", "Get a random image from the Earth Polychromatic Imaging Camera")]
    [SlashCooldown(1, 10, SlashCooldownBucketType.Channel)]
    public async Task GetEarthImageAsync(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        var epic = await _epicService.GetRandomEarthImageAsync();
        var embed = new DiscordEmbedBuilder()
            .WithTitle($"Earth Polychromatic Imaging Camera")
            .WithImageUrl(epic.ImageUrl)
            .WithColor(DiscordColor.Azure)
            .WithFooter($"{epic.Date}")
            .WithDescription("Generating commentary...");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));

        _ = Task.Run(async () =>
        {
            try
            {
                var commentary = await _epicService.GenerateCommentaryAsync(epic);

                embed.WithDescription($"November's commentary: {commentary}");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate commentary.");
            }
        });

    }

    [SlashCommand("neo", "Get today's closest near-Earth object flyby")]
    [SlashCooldown(1, 10, SlashCooldownBucketType.Channel)]
    public async Task GetNeoAsync(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        var neo = await _neoService.GetClosestApproachTodayAsync();

        var hazardLabel = neo.IsPotentiallyHazardous ? "⚠️ Potentially Hazardous" : "Not Hazardous";

        var embed = new DiscordEmbedBuilder()
            .WithTitle($"☄️ {neo.Name}")
            .WithUrl(neo.NasaJplUrl)
            .WithColor(neo.IsPotentiallyHazardous ? DiscordColor.Red : DiscordColor.Azure)
            .AddField("Miss Distance", $"{neo.MissDistanceKm:N0} km ({neo.MissDistanceLunar:N1} LD)", inline: true)
            .AddField("Velocity", $"{neo.VelocityKmPerSecond:N1} km/s", inline: true)
            .AddField("Est. Diameter", $"{neo.DiameterMinMeters:N0}–{neo.DiameterMaxMeters:N0} m", inline: true)
            .AddField("Status", hazardLabel, inline: true)
            .AddField("Orbiting Body", neo.OrbitingBody, inline: true)
            .AddField("Objects Tracked Today", neo.TotalObjectCountToday.ToString(), inline: true)
            .WithFooter($"NASA NeoWs • {neo.CloseApproachDate}")
            .WithDescription("Generating commentary...");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));

        _ = Task.Run(async () =>
        {
            try
            {
                var commentary = await _neoService.GenerateCommentaryAsync(neo);

                embed.WithDescription($"November's commentary: {commentary}");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate commentary.");
            }
        });
    }
}
