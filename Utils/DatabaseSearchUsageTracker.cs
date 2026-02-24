using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VictorNovember.Data;
using VictorNovember.Infrastructure;
using VictorNovember.Interfaces;

namespace VictorNovember.Utils;

public sealed class DatabaseSearchUsageTracker : ISearchUsageTracker
{
    private readonly IDbContextFactory<NovemberContext> _factory;
    private readonly BraveSearchOptions _options;

    public DatabaseSearchUsageTracker(
        IDbContextFactory<NovemberContext> factory,
        IOptions<BraveSearchOptions> options)
    {
        _factory = factory;
        _options = options.Value;
    }

    public async Task<bool> TryAndIncrementAsync(CancellationToken ct = default)
    {
        var monthKey = GetMonthKey();

        await using var db = await _factory.CreateDbContextAsync(ct);

        var affected = await db.Database.ExecuteSqlRawAsync(
            @"UPDATE SearchUsages
              SET Count = Count + 1
              WHERE MonthKey = {0}
              AND Count < {1}",
            new object[] { monthKey, _options.MonthlyLimit },
            ct);

        if (affected > 0)
            return true;

        try
        {
            await db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO SearchUsages (MonthKey, Count)
                  VALUES ({0}, 1)",
                new object[] { monthKey },
                ct);

            return true;
        }
        catch (DbUpdateException)
        {
            // Another thread inserted it. Retry atomic update once.
            affected = await db.Database.ExecuteSqlRawAsync(
                @"UPDATE SearchUsages
                  SET Count = Count + 1
                  WHERE MonthKey = {0}
                  AND Count < {1}",
                new object[] { monthKey, _options.MonthlyLimit },
                ct);

            return affected > 0;
        }
    }

    private static string GetMonthKey()
    {
        var now = DateTime.UtcNow;
        return $"{now.Year}-{now.Month:D2}";
    }
}
