using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System.Diagnostics;
using VictorNovember.Utils;

namespace VictorNovember.ApplicationCommands;

public sealed class General : ApplicationCommandModule
{
    [SlashCommand("avatar", "Get a user's profile picture")]
    public async Task AvatarAsync(InteractionContext ctx,
    [Option("user", "The user to fetch avatar from")] DiscordUser user)
    {
        await ctx.DeferAsync();

        var avatarUrl = user.AvatarUrl;

        var embed = new DiscordEmbedBuilder()
            .WithTitle($"{user.Username}'s avatar")
            .WithImageUrl(avatarUrl)
            .WithColor(DiscordColor.Azure)
            .WithFooter($"Requested by {ctx.User.Username}", ctx.User.AvatarUrl);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [SlashCommand("stats", "Get November's current status")]
    public async Task StatusAsync(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        var process = Process.GetCurrentProcess();
        var uptime = DateTime.Now - process.StartTime;

        var managedMemMb = GC.GetTotalMemory(false) / 1024.0 / 1024.0;

        var totalUsers = ctx.Client.Guilds.Sum(g => g.Value.MemberCount);

        var embed = new DiscordEmbedBuilder()
            .WithTitle("Current Status")
            .WithColor(DiscordColor.Azure)
            .WithThumbnail(ctx.Client.CurrentUser.AvatarUrl)
            .WithFooter($"Stats | Requested by {ctx.User.Username}", ctx.User.AvatarUrl)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .AddField("Ping", $"```fix\n{ctx.Client.Ping}ms```", true)
            .AddField("Total Servers", $"```fix\n{ctx.Client.Guilds.Count}```", true)
            .AddField("Total Users", $"```fix\n{totalUsers}```", true)
            .AddField("Up Time", $"```fix\n{StringUtils.FormatUptime(uptime)}```", true)
            .AddField("Memory Usage", $"```fix\n{managedMemMb:0.0} MB```", true);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [SlashCommand("info", "Get some information on a user")]
    public async Task InfoAsync(InteractionContext ctx,
    [Option("user", "The user to fetch info from")] DiscordUser user)
    {
        await ctx.DeferAsync();

        var embed = new DiscordEmbedBuilder()
            .WithTitle($"{user.Username}'s info")
            .WithColor(DiscordColor.Azure)
            .WithThumbnail(user.AvatarUrl)
            .WithFooter($"Info | Requested by {ctx.User.Username}", ctx.User.AvatarUrl)
            .WithTimestamp(DateTimeOffset.UtcNow);

        embed.AddField("User ID", $"```{user.Id}```", true);
        embed.AddField("Created at", $"```{user.CreationTimestamp:dd/MM/yyyy}```", true);

        // Try to get member info if we're in a guild
        if (ctx.Guild != null && user is DiscordMember member)
        {
            embed.AddField("Join date", $"```{member.JoinedAt:dd/MM/yyyy}```", true);

            var roles = member.Roles
                .Where(r => !r.IsManaged && r.Name != "@everyone")
                .Select(r => r.Mention)
                .ToList();

            var rolesText = roles.Count == 0
                ? "*No roles*"
                : string.Join(" ", roles);

            if (rolesText.Length > 1000)
                rolesText = rolesText.Substring(0, 1000) + "...";

            embed.AddField("Roles", rolesText, false);
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [SlashCommand("serverinfo", "Get the current server's info")]
    [SlashRequireGuild]
    public async Task ServerInfoAsync(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        var guild = ctx.Guild;

        var channels = await guild.GetChannelsAsync();

        var categoryCount = channels.Count(c => c.IsCategory);
        var textCount = channels.Count(c => c.Type == ChannelType.Text);
        var voiceCount = channels.Count(c => c.Type == ChannelType.Voice);

        var roles = guild.Roles.Values
            .Where(r => r.Name != "@everyone")
            .OrderByDescending(r => r.Position)
            .ToList();

        var roleList = roles.Count == 0
            ? "*No roles*"
            : string.Join(" ", roles.Select(r => r.Mention));

        if (roleList.Length > 1000)
            roleList = roleList.Substring(0, 1000) + "...";

        var ownerMember = await guild.GetMemberAsync(guild.OwnerId);
        var owner = ownerMember?.DisplayName ?? $"{guild.OwnerId}";

        var embed = new DiscordEmbedBuilder()
            .WithTitle("Server Info")
            .WithColor(DiscordColor.Azure)
            .WithAuthor(guild.Name, iconUrl: guild.IconUrl)
            .WithThumbnail(guild.IconUrl)
            .WithFooter($"ID: {guild.Id} | Created on {guild.CreationTimestamp:dd/MM/yyyy}")
            .WithTimestamp(DateTimeOffset.UtcNow);

        embed.AddField("Owner", $"```{owner}```", true);
        embed.AddField("Category Channels", $"```{categoryCount}```", true);
        embed.AddField("Text Channels", $"```{textCount}```", true);
        embed.AddField("Voice Channels", $"```{voiceCount}```", true);
        embed.AddField("Member count", $"```{guild.MemberCount}```", true);
        embed.AddField("Roles", $"```{roles.Count}```", true);
        embed.AddField("Role List", roleList, false);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }


}
