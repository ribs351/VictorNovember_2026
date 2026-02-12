using System.ComponentModel.DataAnnotations;

namespace VictorNovember.Data.Entities;

public sealed class Server
{
    [Key]
    public ulong Id { get; set; }
    public string? Prefix { get; set; }
    public ulong? WelcomeChannelId { get; set; }
    public string? WelcomeBannerUrl { get; set; }
}
