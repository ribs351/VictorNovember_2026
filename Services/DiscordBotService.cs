using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VictorNovember.ApplicationCommands;
using VictorNovember.BasicCommands;
using VictorNovember.Services.Welcome;
using VictorNovember.Utils;

namespace VictorNovember.Services;

public sealed class DiscordBotService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly ILogger<DiscordBotService> _logger;

    private DiscordClient? _client;

    public DiscordBotService(
        IServiceProvider services,
        IConfiguration config,
        ILogger<DiscordBotService> logger)
    {
        _services = services;
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var token = _config["Discord:Token"];
        var prefix = _config["Discord:Prefix"] ?? "!";

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Discord bot token missing.");

        var discordConfig = ConfigurationProviderService.GetDiscordConfig(token);
        _client = new DiscordClient(discordConfig);

        _client.Ready += (_, _) =>
        {
            _logger.LogInformation("November is READY and connected.");
            return Task.CompletedTask;
        };

        _client.GuildAvailable += OnGuildBootstrap;
        _client.GuildCreated += OnGuildBootstrap;
        _client.GuildMemberAdded += OnNewGuildMemberAdded;

        var commands = _client.UseCommandsNext(
            ConfigurationProviderService.GetCommandsNextConfig(prefix, _services));
        commands.RegisterCommands<Basic>();

        var slash = _client.UseSlashCommands(
            ConfigurationProviderService.GetSlashCommandsConfig(_services));
        slash.RegisterCommands<GeneralModule>();
        slash.RegisterCommands<FunModule>();
        slash.RegisterCommands<LLMModule>();
        slash.RegisterCommands<ModerationModule>();
        slash.RegisterCommands<WelcomeImageModule>();
        slash.RegisterCommands<NASAModule>();

        _logger.LogInformation("Connecting to Discord...");
        await _client.ConnectAsync(new DiscordActivity("Pondering what to do next...", ActivityType.Playing), UserStatus.DoNotDisturb);
        await slash.RefreshCommands();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_client is not null)
        {
            _logger.LogInformation("Disconnecting from Discord...");
            //_client.GuildMemberAdded -= OnNewGuildMemberAdded;
            await _client.DisconnectAsync();
        }
    }

    private async Task OnGuildBootstrap(DiscordClient s, DSharpPlus.EventArgs.GuildCreateEventArgs e)
    {
        try
        {
            await WhitelistHelper.EnforceGuildWhitelistAsync(s, _config);

            using var scope = _services.CreateScope();
            var tracking = scope.ServiceProvider
                .GetRequiredService<ServerTrackingService>();

            await tracking.CreateServerEntry(e.Guild.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bootstrapping guild {GuildId}", e.Guild.Id);
        }
    }

    private async Task OnNewGuildMemberAdded(DiscordClient s, DSharpPlus.EventArgs.GuildMemberAddEventArgs e)
    {
        try
        {
            using var scope = _services.CreateScope();
            var welcomeService = scope.ServiceProvider.GetRequiredService<WelcomeConfigurationService>();
            var imageRenderer = scope.ServiceProvider.GetRequiredService<WelcomeImageRenderer>();

            var serverWelcomeConfig = await welcomeService.GetConfigAsync(e.Guild.Id);
            if (serverWelcomeConfig is null || serverWelcomeConfig.ChannelId is null)
                return;

            if (!e.Guild.Channels.TryGetValue(serverWelcomeConfig.ChannelId.Value, out var channel))
                return;

            // Generate image
            using var imageStream = await imageRenderer.CreateWelcomeImageAsync(e.Member, serverWelcomeConfig.BackgroundUrl);

            if (channel is DiscordChannel textChannel)
            {
                var messageBuilder = new DiscordMessageBuilder()
                    .WithContent($"Welcome {e.Member.Mention}!")
                    .AddFile("welcome.png", imageStream);

                await textChannel.SendMessageAsync(messageBuilder);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome image for {UserId}", e.Member.Id);
        }
    }
}
