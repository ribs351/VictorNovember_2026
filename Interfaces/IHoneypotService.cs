using DSharpPlus.Entities;

namespace VictorNovember.Interfaces;

public interface IHoneypotService
{
    Task<bool> SetupAsync(DiscordGuild guild, DiscordChannel channel, DiscordChannel modLog, ulong configuredBy);
    Task DisableAsync(ulong guildId);
    Task<Data.Entities.HoneypotConfig?> GetConfigAsync(ulong guildId);
    Task HandleHitAsync(DiscordGuild guild, DiscordMember member, DiscordMessage message);
}