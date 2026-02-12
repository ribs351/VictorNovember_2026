using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VictorNovember.Data;
using VictorNovember.Interfaces;
using VictorNovember.Services.Welcome.Models;

namespace VictorNovember.Services.Welcome;

public sealed class WelcomeConfigurationService : IWelcomeService
{
    private readonly IDbContextFactory<NovemberContext> _dbFactory;
    private readonly IMemoryCache _cache;
    public WelcomeConfigurationService(IDbContextFactory<NovemberContext> dbFactory, IMemoryCache cache)
    {
        _cache = cache;
        _dbFactory = dbFactory;
    }
    private static string CacheKey(ulong guildId)
    => $"welcome:{guildId}";

    public async Task<WelcomeConfigurationResult?> GetConfigAsync(ulong guildId)
    {
        return await _cache.GetOrCreateAsync(
            $"welcome:{guildId}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                await using var db = await _dbFactory.CreateDbContextAsync();
                var server = await db.Servers.FindAsync(guildId);

                return new WelcomeConfigurationResult
                {
                    GuildId = guildId,
                    ChannelId = server?.WelcomeChannelId,
                    BackgroundUrl = server?.WelcomeBannerUrl
                };
            });
    }

    public async Task SetChannelAsync(ulong guildId, ulong channelId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var server = await db.Servers.FindAsync(guildId);
        if (server is null)
            throw new InvalidOperationException($"Server {guildId} not initialized.");

        server.WelcomeChannelId = channelId;
        await db.SaveChangesAsync();

        _cache.Remove(CacheKey(guildId));
    }

    public async Task DisableAsync(ulong guildId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var server = await db.Servers.FindAsync(guildId);
        if (server is null)
            throw new InvalidOperationException($"Server {guildId} not initialized.");

        server.WelcomeBannerUrl = null;
        server.WelcomeChannelId = null;
        await db.SaveChangesAsync();

        _cache.Remove(CacheKey(guildId));
    }

    public async Task SetBackgroundAsync(ulong guildId, string url)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var server = await db.Servers.FindAsync(guildId);
        if (server is null)
            throw new InvalidOperationException($"Server {guildId} not initialized.");

        server.WelcomeBannerUrl = url;
        await db.SaveChangesAsync();

        _cache.Remove(CacheKey(guildId));
    }
    public async Task RemoveBackgroundAsync(ulong guildId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var server = await db.Servers.FindAsync(guildId);
        if (server is null)
            throw new InvalidOperationException($"Server {guildId} not initialized.");

        server.WelcomeBannerUrl = null;
        await db.SaveChangesAsync();

        _cache.Remove(CacheKey(guildId));
    }
}
