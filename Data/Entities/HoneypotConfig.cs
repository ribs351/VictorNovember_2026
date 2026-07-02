namespace VictorNovember.Data.Entities;

public class HoneypotConfig
{
    public ulong GuildId { get; set; } // PK
    public ulong ChannelId { get; set; }
    public ulong ModLogChannelId { get; set; }
    public ulong WarningMessageId { get; set; }
    public ulong CounterMessageId { get; set; }
    public int HitCount { get; set; } = 0;
    public bool Enabled { get; set; } = true;
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
    public ulong ConfiguredByUserId { get; set; }
}