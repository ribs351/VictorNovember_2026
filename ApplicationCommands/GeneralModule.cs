using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System.Diagnostics;
using VictorNovember.ApplicationCommands.Metadata;
using VictorNovember.Utils;

namespace VictorNovember.ApplicationCommands;

public sealed class GeneralModule : ApplicationCommandModule
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

    [SlashCommand("help", "See what November can do")]
    public async Task HelpAsync(InteractionContext ctx)
    {
        await ctx.DeferAsync(ephemeral: true);

        var embed = BuildTopLevelEmbed(ctx.Client);

        var select = BuildCategorySelect();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(embed)
            .AddComponents(select));
    }
    #region Help
    private static DiscordEmbedBuilder BuildTopLevelEmbed(DiscordClient client)
    {
        var embed = new DiscordEmbedBuilder()
            .WithTitle("November — Command Reference")
            .WithDescription("Pick a category below to see the commands in it.")
            .WithColor(DiscordColor.Azure)
            .WithThumbnail(client.CurrentUser.AvatarUrl)
            .WithFooter("Tip: right-click any message and select Apps → Ask November");

        foreach (var (key, category) in Categories)
        {
            embed.AddField(
                category.Name,
                $"{category.Description}\n`{category.Commands.Count} command{(category.Commands.Count == 1 ? "" : "s")}`");
        }

        return embed;
    }

    private static DiscordEmbedBuilder BuildCategoryEmbed(HelpCategory category)
    {
        var embed = new DiscordEmbedBuilder()
            .WithTitle($"{category.Name} Commands")
            .WithDescription(category.Description)
            .WithColor(DiscordColor.Azure)
            .WithFooter($"← Back to categories");

        foreach (var cmd in category.Commands)
        {
            var value = $"`{cmd.Usage}`\n{cmd.Description}";
            if (!string.IsNullOrWhiteSpace(cmd.Example))
                value += $"\n*Example:* `{cmd.Example}`";

            embed.AddField(cmd.Name, value);
        }

        return embed;
    }

    public static async Task OnCategorySelected(
    DiscordClient client,
    ComponentInteractionCreateEventArgs e)
    {
        if (e.Id != "help_category_select")
            return;

        var categoryKey = e.Values.FirstOrDefault();

        if (categoryKey is null)
            return;

        DiscordEmbedBuilder embed;
        DiscordSelectComponent select;

        if (categoryKey == "help_back")
        {
            embed = BuildTopLevelEmbed(client);
            select = BuildCategorySelect();
        }
        else if (Categories.TryGetValue(categoryKey, out var category))
        {
            embed = BuildCategoryEmbed(category);
            select = BuildCategorySelectWithBack();
        }
        else
        {
            return;
        }

        await e.Interaction.CreateResponseAsync(
            InteractionResponseType.UpdateMessage,
            new DiscordInteractionResponseBuilder()
                .AddEmbed(embed)
                .AddComponents(select));
    }

    private static DiscordSelectComponent BuildCategorySelect()
    {
        var options = Categories
            .Select(x => new DiscordSelectComponentOption(
                x.Value.Name,
                x.Key,
                x.Value.Description))
            .ToList();

        return new DiscordSelectComponent(
            "help_category_select",
            "Choose a category...",
            options);
    }

    private static DiscordSelectComponent BuildCategorySelectWithBack()
    {
        var options = new List<DiscordSelectComponentOption>
    {
        new(
            "← Back to categories",
            "help_back",
            "Return to the category list")
    };

        options.AddRange(
            Categories.Select(x => new DiscordSelectComponentOption(
                x.Value.Name,
                x.Key,
                x.Value.Description)));

        return new DiscordSelectComponent(
            "help_category_select",
            "Choose a category...",
            options);
    }

    private static readonly IReadOnlyDictionary<string, HelpCategory> Categories = new Dictionary<string, HelpCategory>
    {
        ["general"] = new(
            "General",
            "Basic info and status",
            new[]
            {
                new HelpCommand(
                    "/help",
                    "Show this command reference.",
                    "/help"),
                new HelpCommand(
                    "/avatar",
                    "Get a user's profile picture.",
                    "/avatar <user>"),
                new HelpCommand(
                    "/info",
                    "Get a user's information.",
                    "/info <user>"),
                new HelpCommand(
                    "/serverinfo",
                    "Get the current server's info.",
                    "/serverinfo"),
                new HelpCommand(
                    "/stats",
                    "Get November's current status.",
                    "/stats"),
            }),

        ["fun"] = new(
            "Fun",
            "Games and casual commands",
            new[]
            {
                new HelpCommand(
                    "/coinflip",
                    "Flip a coin.",
                    "/coinflip"),
                new HelpCommand(
                    "/rps",
                    "Play a game of Rock, Paper, Scissors with November.",
                    "/rps <choice>"),
                new HelpCommand(
                    "/rr",
                    "Play a game of Russian Roulette",
                    "/rr <bullets>")
            }),

        ["space"] = new(
            "Space",
            "NASA imagery and data",
            new[]
            {
                new HelpCommand(
                    "/apod",
                    "Show NASA's Astronomy Picture of the Day.",
                    "/apod"),
                new HelpCommand(
                    "/earthimage",
                    "Get a random image from the Earth Polychromatic Imaging Camera",
                    "/earthimage"),
                new HelpCommand(
                    "/neo",
                    "Get today's closest near-Earth object flyby",
                    "/neo")
            }),

        ["ai"] = new(
            "AI",
            "Chat and text-to-speech",
            new[]
            {
                new HelpCommand(
                    "/llm",
                    "Converse with November.",
                    "/llm <prompt> <mode>"),
                new HelpCommand(
                    "/llm-tts",
                    "Converse with November, with text-to-speech appended (beta).",
                    "/llm-tts <prompt>"),
            }),

        ["moderation"] = new(
            "Moderation",
            "Server management",
            new[]
            {
                new HelpCommand(
                    "/kick",
                    "Kick a user.",
                    "/kick <user> <reason>"),
                new HelpCommand(
                    "/ban",
                    "Ban a user.",
                    "/ban <user> <reason> <delete_days>"),
                new HelpCommand(
                    "/timeout",
                    "Time out a user (default: 2 minutes).",
                    "/timeout <user> <duration> <reason>"
                    ),
                new HelpCommand(
                    "/untimeout",
                    "Remove a user's timeout.",
                    "/untimeout <user> <reason>"
                    ),
                new HelpCommand(
                    "/slowmode",
                    "Set a channel's slowmode interval (in seconds).",
                    "/slowmode <interval>"),
                new HelpCommand(
                    "/purge",
                    "Delete a number of recent messages in a channel (max 100)",
                    "/purge <count>"),
            }),

        ["honeypot"] = new(
            "Honeypot",
            "Spam protection configuration (server owner only)",
            new[]
            {
                new HelpCommand(
                    "/honeypot-setup",
                    "Designate a channel as the honeypot.",
                    "/honeypot-setup <channel> <modlog>"),
                new HelpCommand(
                    "/honeypot-disable",
                    "Disable the honeypot.",
                    "/honeypot-disable"),
                new HelpCommand(
                    "/honeypot-status",
                    "Check honeypot config.",
                    "/honeypot-status"),
            }),

        ["welcome"] = new(
            "Welcome",
            "Configure welcome image generation (requires server management permissions)",
            new[]
            {
                new HelpCommand(
                    "/welcome set-channel",
                    "Set the welcome channel.",
                    "/welcome set-channel <channel>"),
                new HelpCommand(
                    "/welcome set-background",
                    "Set a custom welcome background image.",
                    "/welcome set-background <url>"),
                new HelpCommand(
                    "/welcome disable",
                    "Disable welcome messages.",
                    "/welcome disable"),
                new HelpCommand(
                    "/welcome test",
                    "Test the welcome message with your profile.",
                    "/welcome test"),
            }),

        ["search"] = new(
            "Search",
            "Search the web.",
            new[]
            {
                new HelpCommand(
                    "/search",
                    "Search the web using November.",
                    "/search <query>")
            }),
    };
    #endregion
}
