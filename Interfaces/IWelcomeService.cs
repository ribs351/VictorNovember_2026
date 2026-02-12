using VictorNovember.DTOs;

namespace VictorNovember.Interfaces;

public interface IWelcomeService
{
    Task<WelcomeConfigDTO?> GetConfigAsync(ulong guildId);
    Task SetChannelAsync(ulong guildId, ulong channelId);
    Task DisableAsync(ulong guildId);
    Task SetBackgroundAsync(ulong guildId, string url);
    Task RemoveBackgroundAsync(ulong guildId);
}
