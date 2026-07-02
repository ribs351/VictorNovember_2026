using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using VictorNovember.Interfaces;

namespace VictorNovember.ApplicationCommands;

public class HoneypotModule : ApplicationCommandModule
{
    private readonly IHoneypotService _honeypot;

    public HoneypotModule(IHoneypotService honeypot) => _honeypot = honeypot;

    private static bool IsOwner(InteractionContext ctx) => ctx.Member.Id == ctx.Guild.OwnerId;

    private static Task DenyNotOwner(InteractionContext ctx) =>
        ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent("Only the server owner can configure this.").AsEphemeral());

    [SlashCommand("honeypot-setup", "Designate a channel as the honeypot (server owner only)")]
    public async Task Setup(InteractionContext ctx,
        [Option("channel", "The decoy channel")] DiscordChannel channel,
        [Option("modlog", "Where hits get logged")] DiscordChannel modLog)
    {
        if (!IsOwner(ctx)) { await DenyNotOwner(ctx); return; }

        await ctx.DeferAsync(ephemeral: true);
        await _honeypot.SetupAsync(ctx.Guild, channel, modLog, ctx.Member.Id);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Honeypot set to {channel.Mention}, logging to {modLog.Mention}."));
    }

    [SlashCommand("honeypot-disable", "Disable the honeypot (server owner only)")]
    public async Task Disable(InteractionContext ctx)
    {
        if (!IsOwner(ctx)) { await DenyNotOwner(ctx); return; }

        await _honeypot.DisableAsync(ctx.Guild.Id);
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent("Honeypot disabled.").AsEphemeral());
    }

    [SlashCommand("honeypot-status", "Check honeypot config (server owner only)")]
    public async Task Status(InteractionContext ctx)
    {
        if (!IsOwner(ctx)) { await DenyNotOwner(ctx); return; }

        var config = await _honeypot.GetConfigAsync(ctx.Guild.Id);
        var content = config is null
            ? "No honeypot configured."
            : $"Channel: <#{config.ChannelId}>\nModLog: <#{config.ModLogChannelId}>\nEnabled: {config.Enabled}\nHits: {config.HitCount}\nConfigured: {config.ConfiguredAt:u}";

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent(content).AsEphemeral());
    }
}