using Hangfire;
using Microsoft.EntityFrameworkCore;
using VictorNovember.Data;
using VictorNovember.Interfaces;
using VictorNovember.Jobs;

namespace VictorNovember.Services.Memorial;

public sealed class MemorialService : IMemorialService
{
    // We'll meet again. Don't know where, don't know when.
    // But I know we'll meet again some sunny day...
    private readonly IDbContextFactory<NovemberContext> _dbFactory;
    private readonly IRecurringJobManager _recurringJobs;
    public MemorialService(
        IDbContextFactory<NovemberContext> dbFactory,
        IRecurringJobManager recurringJobs)
    {
        _dbFactory = dbFactory;
        _recurringJobs = recurringJobs;
    }

    public async Task<Data.Entities.Memorial> AddMemorialAsync(
    string personName,
    string message,
    ulong recipientUserId,
    DateOnly anniversaryDate,
    CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var cron = Hangfire.Cron.Yearly(anniversaryDate.Month, anniversaryDate.Day);

        var memorial = new Data.Entities.Memorial
        {
            Id = Guid.NewGuid(),
            PersonName = personName,
            Message = message,
            RecipientUserId = recipientUserId,
            Date = new DateTimeOffset(anniversaryDate.ToDateTime(TimeOnly.MinValue)),
            CronExpression = cron
        };

        db.Memorials.Add(memorial);
        await db.SaveChangesAsync(ct);

        _recurringJobs.AddOrUpdate<MemorialSigil>(
            memorial.Id.ToString(),
            job => job.ExecuteAsync(memorial.PersonName, memorial.Message, memorial.RecipientUserId),
            memorial.CronExpression);

        return memorial;
    }

    public async Task<IReadOnlyList<Data.Entities.Memorial>> GetAllMemorialsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Memorials.ToListAsync(ct);
    }

    public async Task<bool> RemoveMemorialAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var memorial = await db.Memorials.FindAsync([id], ct);
        if (memorial is null)
            return false;

        db.Memorials.Remove(memorial);
        await db.SaveChangesAsync(ct);

        _recurringJobs.RemoveIfExists(id.ToString());

        return true;
    }

    public async Task SyncJobsAsync(CancellationToken ct = default)
    {
        var memorials = await GetAllMemorialsAsync(ct);

        foreach (var memorial in memorials)
        {
            _recurringJobs.AddOrUpdate<MemorialSigil>(
                memorial.Id.ToString(),
                job => job.ExecuteAsync(memorial.PersonName, memorial.Message, memorial.RecipientUserId),
                memorial.CronExpression);
        }
    }
}
