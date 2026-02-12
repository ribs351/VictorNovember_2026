using VictorNovember.Services.Welcome.Models;

namespace VictorNovember.Interfaces;

public interface IWelcomeService
{
    Task<WelcomeConfigurationResult?> GetConfigAsync(ulong guildId);
    Task SetChannelAsync(ulong guildId, ulong channelId);
    Task DisableAsync(ulong guildId);
    Task SetBackgroundAsync(ulong guildId, string url);
    Task RemoveBackgroundAsync(ulong guildId);
}
