namespace VictorNovember.Data.Entities;

public class HoneypotHit
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public string MessageContent { get; set; } = string.Empty;
    public string? AttachmentUrls { get; set; }
    public bool WasBanned { get; set; }
}