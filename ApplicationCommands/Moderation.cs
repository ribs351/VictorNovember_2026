using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using VictorNovember.Utils;

namespace VictorNovember.ApplicationCommands;

public sealed class Moderation : ApplicationCommandModule
{
    #region Kick
    [SlashCommand("kick", "Kick a user")]
    public async Task KickAsync(
        InteractionContext ctx,
        [Option("user", "The user to kick")] DiscordUser user,
        [Option("reason", "Reason for the kick")] string reason = "")
    {
        await ctx.DeferAsync(ephemeral: true);

        if (ctx.Guild is null || ctx.Member is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("This command can only be used in a server."));
            return;
        }

        if (string.IsNullOrEmpty(reason)) 
            reason = $"Kicked by {ctx.User.Username}";
        

        if (!ctx.Member.Permissions.HasPermission(Permissions.KickMembers))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("You don't have permission to kick members."));
            return;
        }

        if (!ctx.Guild.CurrentMember.Permissions.HasPermission(Permissions.KickMembers))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I don't have permission to kick members."));
            return;
        }

        if (user.Id == ctx.Client.CurrentUser.Id)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Kick me yourself, coward!"));
            return;
        }

        if (user.Id == ctx.Member.Id)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("You can't kick yourself."));
            return;
        }
        DiscordMember target;
        try 
        {
            target = await ctx.Guild.GetMemberAsync(user.Id);
        }
        catch
        {
            await ctx.EditResponseAsync (new DiscordWebhookBuilder().WithContent("The user isn't in this server."));
            return;
        }

        if (!PermissionUtils.CanModerateTarget(ctx.Guild, ctx.Member, target, out var err))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(err));
            return;
        }

        try
        {
            await target.RemoveAsync(reason);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Kicked {target.Mention}.\nReason: {reason}"));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong while trying to kick that user. (Missing permissions?)"));
        }
    }
    #endregion

    #region Ban
    [SlashCommand("ban", "Ban a user")]
    public async Task BanAsync(
    InteractionContext ctx,
    [Option("user", "The user to ban")] DiscordUser user,
    [Option("reason", "Reason for the ban")] string reason = "",
    [Option("delete_days", "Delete their message history (0-7 days)")] long deleteDays = 0
)
    {
        await ctx.DeferAsync(ephemeral: true);

        if (ctx.Guild is null || ctx.Member is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("This command can only be used in a server."));
            return;
        }

        // clamp deleteDays into Discord's allowed range
        if (deleteDays < 0) deleteDays = 0;
        if (deleteDays > 7) deleteDays = 7;

        if (string.IsNullOrWhiteSpace(reason))
            reason = $"Banned by {ctx.User.Username}";

        if (!ctx.Member.Permissions.HasPermission(Permissions.BanMembers))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("You don't have permission to ban members."));
            return;
        }

        if (!ctx.Guild.CurrentMember.Permissions.HasPermission(Permissions.BanMembers))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I don't have permission to ban members."));
            return;
        }

        if (user.Id == ctx.Client.CurrentUser.Id)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Ban me yourself, coward!"));
            return;
        }

        if (user.Id == ctx.Member.Id)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("You can't ban yourself."));
            return;
        }

        // If the target is a member, enforce hierarchy checks.
        DiscordMember? target = null;
        try
        {
            target = await ctx.Guild.GetMemberAsync(user.Id);

            if (!PermissionUtils.CanModerateTarget(ctx.Guild, ctx.Member, target, out var err))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(err));
                return;
            }
        }
        catch
        {
            // Not in the server
        }

        try
        {
            // deleteDays must be int (0-7)
            await ctx.Guild.BanMemberAsync(user.Id, (int)deleteDays, reason);

            string name = target is not null ? target.Mention : $"`{user.Username}` ({user.Id})";

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Banned {name}.\nDeleted messages: {deleteDays} day(s).\nReason: {reason}"));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong while trying to ban that user. (Missing permissions?)"));
        }
    }
    #endregion

    #region Timeout
    [SlashCommand("timeout", "Timeout a user (default: 2 minutes)")]
    public async Task TimeoutAsync(
    InteractionContext ctx,
    [Option("user", "The user to timeout")] DiscordUser user,
    [Option("duration", "Duration in seconds (10 - 2419200)")] long durationSeconds = 120,
    [Option("reason", "Reason for the timeout")] string reason = ""
)
    {
        await ctx.DeferAsync(ephemeral: true);

        if (ctx.Guild is null || ctx.Member is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("This command can only be used in a server."));
            return;
        }

        if (string.IsNullOrWhiteSpace(reason))
            reason = $"Timed out by {ctx.User.Username}";

        const long minSeconds = 10;
        const long maxSeconds = 28 * 24 * 60 * 60; // 2419200

        if (durationSeconds < minSeconds) durationSeconds = minSeconds;
        if (durationSeconds > maxSeconds) durationSeconds = maxSeconds;

        if (!ctx.Member.Permissions.HasPermission(Permissions.ModerateMembers))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("You don't have permission to timeout members."));
            return;
        }

        if (!ctx.Guild.CurrentMember.Permissions.HasPermission(Permissions.ModerateMembers))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I don't have permission to timeout members."));
            return;
        }

        if (user.Id == ctx.Client.CurrentUser.Id)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I will not be silenced."));
            return;
        }

        if (user.Id == ctx.Member.Id)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("You can't timeout yourself."));
            return;
        }

        DiscordMember target;
        try
        {
            target = await ctx.Guild.GetMemberAsync(user.Id);
        }
        catch
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("That user isn't in this server."));
            return;
        }

        if (!PermissionUtils.CanModerateTarget(ctx.Guild, ctx.Member, target, out var err))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(err));
            return;
        }

        try
        {
            var until = DateTimeOffset.UtcNow.AddSeconds(durationSeconds);

            await target.TimeoutAsync(until, reason);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(
                    $"Timed out {target.Mention} for **{TimeSpan.FromSeconds(durationSeconds)}**.\nReason: {reason}"
                ));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong while trying to timeout that user. (Missing permissions?)"));
        }
    }
    #endregion

    #region Timeout_Release
    [SlashCommand("untimeout", "Remove a user's timeout")]
    public async Task UnTimeoutAsync(
    InteractionContext ctx,
    [Option("user", "The user to remove timeout from")] DiscordUser user,
    [Option("reason", "Reason for removing timeout")] string reason = ""
)
    {
        await ctx.DeferAsync(ephemeral: true);

        if (ctx.Guild is null || ctx.Member is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("This command can only be used in a server."));
            return;
        }

        if (string.IsNullOrWhiteSpace(reason))
            reason = $"Timeout removed by {ctx.User.Username}";

        if (!ctx.Member.Permissions.HasPermission(Permissions.ModerateMembers))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("You don't have permission to remove timeouts."));
            return;
        }

        if (!ctx.Guild.CurrentMember.Permissions.HasPermission(Permissions.ModerateMembers))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I don't have permission to remove timeouts."));
            return;
        }

        if (user.Id == ctx.Client.CurrentUser.Id)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Nice try."));
            return;
        }

        DiscordMember target;
        try
        {
            target = await ctx.Guild.GetMemberAsync(user.Id);
        }
        catch
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("That user isn't in this server."));
            return;
        }

        if (!PermissionUtils.CanModerateTarget(ctx.Guild, ctx.Member, target, out var err))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(err));
            return;
        }

        try
        {
            await target.TimeoutAsync(null, reason);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Removed timeout from {target.Mention}.\nReason: {reason}"));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong while trying to remove that timeout. (Missing permissions?)"));
        }
    }
    #endregion

    #region Slowmode
    [SlashCommand("slowmode", "Set this channel's slowmode interval (in seconds).")]
    public async Task SlowModeAsync(
    InteractionContext ctx,
    [Option("interval", "Slowmode in seconds (0 = off, max 21600).")] long interval
)
    {
        await ctx.DeferAsync(ephemeral: true);

        if (ctx.Guild is null || ctx.Member is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("This command can only be used in a server."));
            return;
        }

        if (!ctx.Member.Permissions.HasPermission(Permissions.ManageChannels))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("You don't have permission to manage channels."));
            return;
        }

        if (!ctx.Guild.CurrentMember.Permissions.HasPermission(Permissions.ManageChannels))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I don't have permission to manage channels."));
            return;
        }

        // 6 hours max
        interval = Math.Clamp(interval, 0, 21600);

        try
        {
            await ctx.Channel.ModifyAsync(x => x.PerUserRateLimit = (int)interval);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Slowmode set to **{interval} seconds** in {ctx.Channel.Mention}."));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong while trying to update slowmode. (Missing permissions?)"));
        }
    }
    #endregion

    #region Purge
    private const int MaxPurgeAmount = 100;
    [SlashCommand("purge", "Delete a number of recent messages in this channel (max 100).")]
    public async Task PurgeAsync(
        InteractionContext ctx,
        [Option("amount", "How many messages to delete (1-100).")] long amount
    )
    {
        await ctx.DeferAsync();

        if (ctx.Guild is null || ctx.Member is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("This command can only be used in a server."));
            return;
        }

        if (!ctx.Member.Permissions.HasPermission(Permissions.ManageMessages))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("You don't have permission to manage messages."));
            return;
        }

        if (!ctx.Guild.CurrentMember.Permissions.HasPermission(Permissions.ManageMessages))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("I don't have permission to manage messages."));
            return;
        }

        if (amount <= 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Amount must be at least 1."));
            return;
        }

        if (amount > MaxPurgeAmount)
            amount = MaxPurgeAmount;

        try
        {
            // Grab a bit extra in case pinned/old messages get filtered out
            var fetched = await ctx.Channel.GetMessagesAsync((int)amount + 5);

            // Cannot delete messages older than 14 days
            var cutoff = DateTimeOffset.UtcNow.AddDays(-14);

            var toDelete = fetched
                .Where(m => m.Timestamp > cutoff)
                .Where(m => !m.Pinned)
                .Where(m => m.Author?.Id != ctx.Client.CurrentUser.Id)
                .Take((int)amount)
                .ToList();

            if (toDelete.Count == 0)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("No messages could be deleted (too old or pinned)."));
                return;
            }

            if (toDelete.Count == 1)
            {
                await ctx.Channel.DeleteMessageAsync(toDelete[0]);
            }
            else
            {
                await ctx.Channel.DeleteMessagesAsync(toDelete);
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Deleted **{toDelete.Count}** messages in {ctx.Channel.Mention}."));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong while trying to purge messages."));
        }
    }
    #endregion
}
