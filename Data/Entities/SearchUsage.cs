namespace VictorNovember.Data.Entities;

public sealed class SearchUsage
{
    public int Id { get; set; }

    // Format: "2026-02"
    public string MonthKey { get; set; } = string.Empty;

    public int Count { get; set; }
}
