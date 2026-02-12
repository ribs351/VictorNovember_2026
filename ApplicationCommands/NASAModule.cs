using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using VictorNovember.Interfaces;

namespace VictorNovember.ApplicationCommands;

public sealed class NASAModule : ApplicationCommandModule
{
    private readonly IApodService _apodService;
    private readonly ILogger<NASAModule> _logger;
    public NASAModule(IApodService apodService, ILogger<NASAModule> logger)
    {
        _apodService = apodService;
        _logger = logger;
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

                embed.WithDescription(commentary);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate commentary.");
            }
        });
    }
}
