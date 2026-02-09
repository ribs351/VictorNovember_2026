using DSharpPlus;
using Microsoft.Extensions.Configuration;

namespace VictorNovember.Utils;

public static class WhitelistHelper
{
    public static async Task EnforceGuildWhitelistAsync(DiscordClient client, IConfiguration config)
    {
        var whitelist = config.GetSection("GuildWhitelist")
            .Get<ulong[]>()?
            .ToHashSet() ?? new HashSet<ulong>();

        var guilds = client.Guilds.Values.ToList(); // snapshot

        foreach (var guild in guilds)
        {
            if (whitelist.Contains(guild.Id))
                continue;

            try
            {
                // Try to find a channel we can speak in
                var channel = guild.GetDefaultChannel();

                if (channel != null)
                {
                    try
                    {
                        await channel.SendMessageAsync(
                            "Hi! This bot is currently in private testing and is only allowed in a small whitelist of servers.\n" +
                            "So I’m going to leave this server automatically.\n\n" +
                            "If you think this is a mistake, contact the bot owner."
                        );
                    }
                    catch
                    {
                        // Can't message. Not fatal.
                    }
                }

                await guild.LeaveAsync();
                Console.WriteLine($"Left non-whitelisted guild: {guild.Name} ({guild.Id})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to leave guild {guild.Name} ({guild.Id}): {ex.Message}");
            }
        }
    }


}
