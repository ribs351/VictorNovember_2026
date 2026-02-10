using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.SlashCommands;

namespace VictorNovember.Services;

public static class ConfigurationProviderService
{
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
    public static CommandsNextConfiguration GetCommandsNextConfig(string prefix, IServiceProvider services)
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

    public static SlashCommandsConfiguration GetSlashCommandsConfig(IServiceProvider services)
    {
        return new SlashCommandsConfiguration
        {
            Services = services
        };
    }
}
