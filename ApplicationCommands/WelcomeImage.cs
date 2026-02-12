using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.DependencyInjection;
using VictorNovember.Services.Welcome;

namespace VictorNovember.ApplicationCommands;

[SlashCommandGroup("welcome", "Configure welcome image generation.")]
[SlashRequireGuild]
[SlashRequireUserPermissions(Permissions.ManageGuild)]
public class WelcomeImage : ApplicationCommandModule
{
    private readonly WelcomeConfigurationService _configService;
    private readonly WelcomeImageRenderer _renderder;
    public WelcomeImage(WelcomeConfigurationService configService, WelcomeImageRenderer renderer)
    {
        _configService = configService;
        _renderder = renderer;
    }
    

    [SlashCommand("set-channel", "Set the welcome channel")]
    public async Task SetChannel(
        InteractionContext ctx,
        [Option("channel", "The channel for welcome messages")] DiscordChannel channel)
    {
        await ctx.DeferAsync(ephemeral: true);

        var config = await _configService.GetConfigAsync(ctx.Guild.Id);

        if (config == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Server not found in database. Contact bot owner."));
            return;
        }

        await _configService.SetChannelAsync(config.GuildId, channel.Id);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Welcome messages will be sent to {channel.Mention}"));
    }

    [SlashCommand("set-background", "Set a custom welcome background image")]
    public async Task SetBackground(
        InteractionContext ctx,
        [Option("url", "Direct image URL (must be publicly accessible)")] string url)
    {
        await ctx.DeferAsync(ephemeral: true);

        // Validate URL is an image
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !url.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
            !url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) &&
            !url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Invalid image URL. Must be a direct link to a .png or .jpg file."));
            return;
        }

        var config = await _configService.GetConfigAsync(ctx.Guild.Id);

        if (config == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Server not found in database. Contact bot owner."));
            return;
        }

        await _configService.SetBackgroundAsync(config.GuildId, url);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Welcome background updated! Preview: {url}"));
    }

    [SlashCommand("disable", "Disable welcome messages")]
    public async Task Disable(InteractionContext ctx)
    {
        await ctx.DeferAsync(ephemeral: true);

        var config = await _configService.GetConfigAsync(ctx.Guild.Id);

        if (config == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Server not found in database. Contact bot owner."));
            return;
        }

        await _configService.DisableAsync(config.GuildId);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Welcome messages disabled."));
    }

    [SlashCommand("test", "Test the welcome message with your profile")]
    public async Task Test(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        var config = await _configService.GetConfigAsync(ctx.Guild.Id);

        var backgroundUrl = config == null ? null : config.BackgroundUrl;

        var imageStream = await _renderder.CreateWelcomeImageAsync(
            ctx.Member,
            backgroundUrl);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddFile("welcome.png", imageStream)
            .WithContent("Here's how your welcome message looks:"));

        imageStream.Dispose();
    }

    //private readonly IServiceScopeFactory _scopeFactory;
    //public WelcomeImage(IServiceScopeFactory scopeFactory)
    //{
    //    _scopeFactory = scopeFactory;
    //}

    //[SlashCommand("test2", "Test the welcome message")]
    //public async Task Test2(InteractionContext ctx)
    //{
    //    using var scope = _scopeFactory.CreateScope();
    //    var configService = scope.ServiceProvider
    //        .GetRequiredService<WelcomeConfigurationService>();

    //    await ctx.CreateResponseAsync("Hit test command.");
    //}
}
