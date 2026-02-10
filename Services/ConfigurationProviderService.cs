using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace VictorNovember.Services;

public static class ConfigurationProviderService
{
    public static IConfigurationRoot Build()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Path.Combine(AppContext.BaseDirectory, "Config"))
            .AddJsonFile("config.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    public static DiscordConfiguration GetDiscordConfig(string token)
    {
        return new DiscordConfiguration()
        {
            Intents = DiscordIntents.Guilds
                    | DiscordIntents.GuildMembers
                    | DiscordIntents.GuildMessages
                    | DiscordIntents.MessageContents,
            Token = token,
            TokenType = TokenType.Bot,
            AutoReconnect = true
        };
    }
    public static CommandsNextConfiguration GetCommandsNextConfig(string prefix, ServiceProvider services)
    {
        return new CommandsNextConfiguration
        {
            StringPrefixes = new string[] { prefix },
            EnableMentionPrefix = true,
            EnableDms = true,
            EnableDefaultHelp = false,
            Services = services
        };
    }

    public static SlashCommandsConfiguration GetSlashCommandsConfig(ServiceProvider services)
    {
        return new SlashCommandsConfiguration
        {
            Services = services
        };
    }
}
