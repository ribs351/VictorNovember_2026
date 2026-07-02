using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VictorNovember.Data;
using VictorNovember.Data.Entities;
using VictorNovember.Interfaces;

namespace VictorNovember.Services;

public sealed class HoneypotService : IHoneypotService
{
    private readonly IDbContextFactory<NovemberContext> _dbFactory;
    private readonly ILogger<HoneypotService> _logger;

    public HoneypotService(IDbContextFactory<NovemberContext> dbFactory, ILogger<HoneypotService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    private static DiscordButtonComponent BuildCounterButton(int count) =>
        new(DSharpPlus.ButtonStyle.Secondary, "honeypot_counter", $"🐝 Bans: {count}", disabled: true);

    public async Task<bool> SetupAsync(DiscordGuild guild, DiscordChannel channel, DiscordChannel modLog, ulong configuredBy)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var config = await db.HoneypotConfigs.FindAsync(guild.Id);

        var warningEmbed = new DiscordEmbedBuilder()
            .WithTitle("⚠️ DO NOT POST IN THIS CHANNEL")
            .WithDescription("This channel is monitored. Posting here — for any reason — will result in an immediate ban.\n\nIf you've reached this channel by mistake, simply leave it alone.")
            .WithColor(DiscordColor.Red)
            .Build();

        var warningMsg = await channel.SendMessageAsync(warningEmbed);
        await warningMsg.PinAsync();

        var counterEmbed = new DiscordEmbedBuilder()
            .WithTitle("Honeypot Stats")
            .WithColor(DiscordColor.Gold)
            .Build();

        var counterMsg = await channel.SendMessageAsync(new DiscordMessageBuilder()
            .AddEmbed(counterEmbed)
            .AddComponents(BuildCounterButton(config?.HitCount ?? 0)));

        if (config is null)
        {
            config = new HoneypotConfig { GuildId = guild.Id };
            db.HoneypotConfigs.Add(config);
        }

        config.ChannelId = channel.Id;
        config.ModLogChannelId = modLog.Id;
        config.WarningMessageId = warningMsg.Id;
        config.CounterMessageId = counterMsg.Id;
        config.Enabled = true;
        config.ConfiguredAt = DateTime.UtcNow;
        config.ConfiguredByUserId = configuredBy;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task DisableAsync(ulong guildId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var config = await db.HoneypotConfigs.FindAsync(guildId);
        if (config is null) return;

        config.Enabled = false;
        await db.SaveChangesAsync();
    }

    public async Task<HoneypotConfig?> GetConfigAsync(ulong guildId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.HoneypotConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.GuildId == guildId);
    }

    public async Task HandleHitAsync(DiscordGuild guild, DiscordMember member, DiscordMessage message)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var config = await db.HoneypotConfigs.FirstOrDefaultAsync(c => c.GuildId == guild.Id && c.Enabled);
        if (config is null) return;

        var attachmentUrls = message.Attachments.Select(a => a.Url).ToList();

        var hit = new HoneypotHit
        {
            GuildId = guild.Id,
            UserId = member.Id,
            Username = member.Username,
            JoinedAt = member.JoinedAt.UtcDateTime,
            MessageContent = message.Content,
            AttachmentUrls = attachmentUrls.Count > 0 ? string.Join(";", attachmentUrls) : null,
            WasBanned = true
        };
        db.HoneypotHits.Add(hit);

        config.HitCount++;

        await db.SaveChangesAsync();

        try
        {
            if (guild.Channels.TryGetValue(config.ModLogChannelId, out var modLogChannel))
            {
                var embed = new DiscordEmbedBuilder()
                    .WithTitle("Honeypot Triggered")
                    .WithColor(DiscordColor.Red)
                    .AddField("User", $"{member.Mention} (`{member.Id}`)")
                    .AddField("Joined Server", member.JoinedAt.ToString("u"))
                    .AddField("Message", string.IsNullOrWhiteSpace(message.Content) ? "*(no text, attachment only)*" : message.Content)
                    .AddField("Record ID", hit.Id.ToString())
                    .WithTimestamp(DateTimeOffset.UtcNow)
                    .Build();

                await modLogChannel.SendMessageAsync(embed);
            }

            if (guild.Channels.TryGetValue(config.ChannelId, out var honeypotChannel))
            {
                var counterMsg = await honeypotChannel.GetMessageAsync(config.CounterMessageId);
                await counterMsg.ModifyAsync(new DiscordMessageBuilder()
                    .AddEmbed(counterMsg.Embeds[0])
                    .AddComponents(BuildCounterButton(config.HitCount)));
            }

            await message.DeleteAsync();
            await member.BanAsync(reason: $"Honeypot trigger — hit #{hit.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing honeypot hit for guild {GuildId}, user {UserId}", guild.Id, member.Id);
        }
    }
}