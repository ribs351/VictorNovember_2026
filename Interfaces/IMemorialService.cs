using VictorNovember.Data.Entities;

namespace VictorNovember.Interfaces;

public interface IMemorialService
{
    Task<IReadOnlyList<Memorial>> GetAllMemorialsAsync(CancellationToken ct = default);
    Task<Memorial> AddMemorialAsync(
        string personName,
        string message,
        ulong recipientUserId,
        DateOnly anniversaryDate,
        CancellationToken ct = default);
    Task<bool> RemoveMemorialAsync(Guid id, CancellationToken ct = default);
    Task SyncJobsAsync(CancellationToken ct = default);
}
