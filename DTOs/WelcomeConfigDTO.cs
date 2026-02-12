namespace VictorNovember.DTOs;

public sealed class WelcomeConfigDTO
{
    public ulong GuildId { get; init; }
    public ulong? ChannelId { get; init; }
    public string? BackgroundUrl { get; init; }

    public bool IsEnabled => ChannelId is not null;
}
