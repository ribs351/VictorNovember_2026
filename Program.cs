using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VictorNovember.ApplicationCommands;
using VictorNovember.BasicCommands;
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
        var configuration = new ConfigurationBuilder()
        .SetBasePath($"{AppContext.BaseDirectory}/Config")
        .AddJsonFile("config.json", optional: false, reloadOnChange: true)
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables()
        .Build();

        var token = configuration["Discord:Token"];
        var prefix = configuration["Discord:Prefix"] ?? "!";

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Discord bot token missing. Set Discord:Token in config.json.");

        var services = new ServiceCollection()
        .AddSingleton<IConfiguration>(configuration)
        .AddSingleton<GoogleGeminiService>()
        .AddMemoryCache()
        .BuildServiceProvider();

        var config = new DiscordConfiguration()
        {
            Intents = DiscordIntents.Guilds
                    | DiscordIntents.GuildMembers
                    | DiscordIntents.GuildMessages
                    | DiscordIntents.MessageContents,
            Token = token,
            TokenType = TokenType.Bot,
            AutoReconnect = true
        };

        Client = new DiscordClient(config);

        Client.Ready += Client_Ready;
        // To be removed later when it's ready
        Client.GuildAvailable += async (s, e) =>
        {
            await WhitelistHelper.EnforceGuildWhitelistAsync(s, configuration);
        };

        Client.GuildCreated += async (s, e) =>
        {
            await WhitelistHelper.EnforceGuildWhitelistAsync(s, configuration);
        };

        Commands = Client.UseCommandsNext(new CommandsNextConfiguration
        {
            StringPrefixes = new string[] { prefix },
            EnableMentionPrefix = true,
            EnableDms = true,
            EnableDefaultHelp = false,
            Services = services
        });
        Commands.RegisterCommands<Basic>();

        Slash = Client.UseSlashCommands(new SlashCommandsConfiguration
        {
            Services = services
        });
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
