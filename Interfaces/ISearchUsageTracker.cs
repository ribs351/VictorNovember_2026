namespace VictorNovember.Interfaces;

public interface ISearchUsageTracker
{
    Task<bool> TryAndIncrementAsync(CancellationToken ct = default);
}
