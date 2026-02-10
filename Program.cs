using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VictorNovember.ApplicationCommands;
using VictorNovember.BasicCommands;
using VictorNovember.Data;
using VictorNovember.Services;
using VictorNovember.Utils;

namespace VictorNovember;

public sealed class Program
{
    public static DiscordClient? Client { get; set; }
    public static CommandsNextExtension? Commands { get; set; }
    public static SlashCommandsExtension? Slash { get; set; }
    static async Task Main(string[] args)
    {
        var configuration = ConfigurationProviderService.Build();
        var token = configuration["Discord:Token"];
        var prefix = configuration["Discord:Prefix"] ?? "!";

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Discord bot token missing. Set Discord:Token in config.json.");

        var services = new ServiceCollection()
        .AddSingleton<IConfiguration>(configuration)
        .AddSingleton<GoogleGeminiService>()
        .AddMemoryCache()
        .AddDbContext<NovemberContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("NovemberDb"));
        })
        .AddScoped<ServerBootstrapService>()
        .BuildServiceProvider();

        var discordConfig = ConfigurationProviderService.GetDiscordConfig(token);

        Client = new DiscordClient(discordConfig);

        Client.Ready += Client_Ready;
        
        Client.GuildAvailable += async (s, e) =>
        {
            await WhitelistHelper.EnforceGuildWhitelistAsync(s, configuration); // To be removed later when it's ready
            using var scope = services.CreateScope();
            var bootstrap = scope.ServiceProvider
                .GetRequiredService<ServerBootstrapService>();

            await bootstrap.CreateServerEntry(e.Guild.Id);
        };

        Client.GuildCreated += async (s, e) =>
        {
            await WhitelistHelper.EnforceGuildWhitelistAsync(s, configuration); // To be removed later when it's ready

            using var scope = services.CreateScope();
            var bootstrap = scope.ServiceProvider
                .GetRequiredService<ServerBootstrapService>();

            await bootstrap.CreateServerEntry(e.Guild.Id);
        };

        Commands = Client.UseCommandsNext(ConfigurationProviderService.GetCommandsNextConfig(prefix, services));
        Commands.RegisterCommands<Basic>();

        Slash = Client.UseSlashCommands(ConfigurationProviderService.GetSlashCommandsConfig(services));
        Slash.RegisterCommands<General>();
        Slash.RegisterCommands<Fun>();
        Slash.RegisterCommands<LLM>();
        Slash.RegisterCommands<Moderation>();

        Console.WriteLine("Establishing connection to discord, standby...");

        await Client.ConnectAsync(new DiscordActivity("Pondering what to do next...", ActivityType.Playing), UserStatus.Idle);
        await Task.Delay(Timeout.Infinite);
    }

    private static Task Client_Ready(DiscordClient sender, ReadyEventArgs args) => Task.CompletedTask;
}
