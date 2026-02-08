using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Exceptions;

namespace VictorNovember.BasicCommands;

public class Basic : BaseCommandModule
{
    [Command("ping")]
    public async Task PingCommand(CommandContext ctx)
    {
        await ctx.TriggerTypingAsync();
        await ctx.Channel.SendMessageAsync($"Bot connected with an expected ping of `{ctx.Client.Ping} ms`");
    }

    [Command("say")]
    [Description("Makes the bot repeat your message.")]
    [RequirePermissions(DSharpPlus.Permissions.ManageMessages)]
    public async Task Say(CommandContext ctx, [RemainingText] string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            await ctx.RespondAsync("Say what?");
            return;
        }

        // Block common abuse
        if (text.Contains("@everyone", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("@here", StringComparison.OrdinalIgnoreCase))
        {
            await ctx.RespondAsync("No mass pings!");
            return;
        }

        // Block real mention syntax
        if (text.Contains("<@") || text.Contains("<@&") || text.Contains("<#"))
        {
            await ctx.RespondAsync("No mentions!");
            return;
        }

        // Try deleting the command message
        try
        {
            await ctx.Message.DeleteAsync();
        }
        catch (UnauthorizedException) { }
        await ctx.TriggerTypingAsync();
        await ctx.Channel.SendMessageAsync(new DSharpPlus.Entities.DiscordMessageBuilder()
            .WithContent(text)
            .WithAllowedMentions(DSharpPlus.Entities.Mentions.None));
    }
}
