using Microsoft.EntityFrameworkCore;
using VictorNovember.Data;
using VictorNovember.Data.Entities;

namespace VictorNovember.Services;

public sealed class ServerTrackingService
{
    private readonly NovemberContext _db;
    public ServerTrackingService(NovemberContext db)
    {
        _db = db;
    }

    public async Task CreateServerEntry(ulong guildId)
    {
        var exists = await _db.Servers
            .AsNoTracking()
            .AnyAsync(s => s.Id == guildId);

        if (exists)
            return;

        _db.Servers.Add(new Server
        {
            Id = guildId,
            Prefix = "!"
        });

        await _db.SaveChangesAsync();
    }
}
