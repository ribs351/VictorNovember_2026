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
            Console.WriteLine("November is READY and connected.");
            return Task.CompletedTask;
        };

        _client.GuildAvailable += OnGuildBootstrap;
        _client.GuildCreated += OnGuildBootstrap;
        //_client.GuildMemberAdded += OnNewGuildMemberAdded;

        var commands = _client.UseCommandsNext(
            ConfigurationProviderService.GetCommandsNextConfig(prefix, _services));
        commands.RegisterCommands<Basic>();

        var slash = _client.UseSlashCommands(
            ConfigurationProviderService.GetSlashCommandsConfig(_services));
        slash.RegisterCommands<General>();
        slash.RegisterCommands<Fun>();
        slash.RegisterCommands<LLM>();
        slash.RegisterCommands<Moderation>();

        _logger.LogInformation("Connecting to Discord...");
        await _client.ConnectAsync(new DiscordActivity("Pondering what to do next...", ActivityType.Playing), UserStatus.DoNotDisturb);
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
            var bootstrap = scope.ServiceProvider
                .GetRequiredService<ServerBootstrapService>();

            await bootstrap.CreateServerEntry(e.Guild.Id);
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
            var welcomeService = scope.ServiceProvider.GetRequiredService<WelcomeImageService>();

            // Generate image
            using var imageStream = await welcomeService.CreateWelcomeImageAsync(e.Member);

            var welcomeChannelId = await welcomeService.GetWelcomeChannelIdAsync(e.Guild.Id);
            if (welcomeChannelId is null)
                return;

            if (!e.Guild.Channels.TryGetValue(welcomeChannelId.Value, out var channel))
                return;

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
