namespace VictorNovember.Data.Entities;

public sealed class Memorial
{
    // We'll pay homage to the fallen
    public Guid Id { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public string Message { get; set; } = string.Empty;
    public ulong RecipientUserId { get; set; }
    public string CronExpression { get; set; } = string.Empty;
}
